using Opc.Ua.Toolkit.Trace;
using System;
using System.Collections.Generic;
using System.Text;

namespace Opc.Ua.Toolkit.Client
{
    /// <summary>
    /// Represents the base class for the managing the state of the Session, Subscription, MonitoredItem.
    /// </summary>
    public abstract class BaseStateManagement
    {
        #region Private Fields

        private readonly static object s_targetStateSync = new object();
        private readonly object m_stateTransitionSync = new object();

        private volatile State m_currentState = State.Disconnected;
        private volatile State m_targetState = State.Disconnected;

        private BaseStateManagement m_parent;

        #endregion Private Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseStateManagement"/> class.
        /// </summary>
        /// <param name="parent">The parent object.</param>
        /// <include file='Doc\Client\BaseStateManagement.xml' path='class[@name="BaseStateManagement"]/constructor[@name="BaseStateManagement"]/*'/>
        protected BaseStateManagement(BaseStateManagement parent)
        {
            m_parent = parent;
        }

        #endregion Constructors

        #region Public Events

        /// <summary>
        /// Occurs when the state of the current BaseStateManagement instance has changed.
        /// </summary>
        public event EventHandler StateChanged;

        #endregion Public Events

        #region Public Properties

        /// <summary>
        /// Gets the current state.<br/>
        /// The current state represents the actual state of the BaseStateManagement object.
        /// </summary>
        public virtual State CurrentState
        {
            get { return m_currentState; }
            protected internal set { m_currentState = value; }
        }

        /// <summary>
        /// Gets the target state.<br/>
        /// The target state represents the desired state for the BaseStateManagement object.
        /// </summary>
        public virtual State TargetState
        {
            get { return m_targetState; }
            protected internal set { m_targetState = value; }
        }

        #endregion Public Properties

        #region Internal Properties

        /// <summary>
        /// Gets state transition synchronization object.<br/>
        /// This object should only be locked when performing internal SDK object-related operations.
        /// </summary>
        internal object StateTransitionSync
        {
            get { return m_stateTransitionSync; }
        }

        #endregion Internal Properties

        #region Protected Internal Properties

        /// <summary>
        /// Gets or sets the parent of this instance.
        /// </summary>
        protected internal BaseStateManagement Parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }

        #endregion Protected Internal Properties

        #region Public Methods

        /// <summary>
        /// Connects the object and creates any required items on the server, also for child items.
        /// </summary>
        /// <param name="deep">If set to <c>true</c>, all the children target states are also updated.</param>
        /// <param name="active">If set to <c>true</c>, the target state will be set to active, otherwise is will be set to simply connected.</param>
        /// <include file='Doc\Client\BaseStateManagement.xml' path='class[@name="BaseStateManagement"]/property[@name="Connect"]/*'/>
        public void Connect(bool deep, bool active)
        {
            var targetState = active ? State.Active : State.Connected;

            lock (s_targetStateSync)
            {
                TargetState = targetState;

                if (deep)
                {
                    SetChildrenTargetState(targetState);
                }
            }

            PerformStateTransition(false);
            SetModified();
        }

        /// <summary>
        /// Disconnects the object and deletes the server object if required. All child items will also be deleted from the server.<br/>
        /// </summary>
        /// <param name="deep">If set to <c>true</c>, all the children's target states are also disconnected.</param>
        /// <include file='Doc\Client\BaseStateManagement.xml' path='class[@name="BaseStateManagement"]/property[@name="Disconnect"]/*'/>
        public void Disconnect(bool deep)
        {
            lock (s_targetStateSync)
            {
                TargetState = State.Disconnected;

                if (deep)
                {
                    SetChildrenTargetState(State.Disconnected);
                }
            }

            PerformStateTransition(false);
            SetModified();
        }

        #endregion Public Methods

        #region Internal Methods

        /// <summary>
        /// This method should be implemented in inheriting classes and should perform all actions required to advance the element to the target state.<br/>
        /// This method is called from the <see cref="PerformStateTransition"/> method.<br/>
        /// This method should not throw any exception.
        /// </summary>
        /// <param name="targetState">Represents the target state to advance to.</param>
        /// <param name="reconnecting">Identifies whether this is a reconnecting call issued by the reconnect handler or not</param>
        internal virtual void InternalConnect(State targetState, bool reconnecting)
        { }

        /// <summary>
        /// This method should be implemented in inheriting classes and should perform all actions required to advance the element to the disconnected state.<br/>
        /// This method is called from the <see cref="PerformStateTransition"/> method.<br/>
        /// This method should not throw any exception.
        /// </summary>
        /// <param name="reconnecting">Identifies whether this is a reconnecting call issued by the reconnect handler or not.</param>
        internal virtual void InternalDisconnect(bool reconnecting)
        { }

        /// <summary>
        /// This method should be implemented in the inheriting classes.<br/>
        /// Should disable the reconnect handler before performing a state transition (mainly on the session object).
        /// </summary>
        internal virtual void DisableReconnectHandler()
        { }

        /// <summary>
        /// This method should be implemented in the inheriting classes.<br/>
        /// Should enable the reconnect handler in case of connect/activate failure (call should be raised to the session object).
        /// </summary>
        internal virtual void EnableReconnectHandler()
        {
            if (Parent != null)
            {
                Parent.EnableReconnectHandler();
            }
        }

        /// <summary>
        /// Marks the current session as modified if any operation was performed while the session target state was connected/active and its current state was disconnected.<br/>
        /// </summary>
        /// <include file='Doc\Client\BaseStateManagement.xml' path='class[@name="BaseStateManagement"]/property[@name="SetModified"]/*'/>
        internal virtual void SetModified()
        {
            if (Parent != null)
            {
                Parent.SetModified();
            }
        }

        /// <summary>
        /// Returns a list of BaseStateManagement children of the current BaseStateManagement instance (only relevant for session/subscription).
        /// </summary>
        /// <returns></returns>
        internal virtual List<BaseStateManagement> GetChildren()
        {
            return new List<BaseStateManagement>();
        }

        #endregion Internal Methods

        #region Protected Methods

        /// <summary>
        /// Raises the state changed event (the change of the current state).
        /// </summary>
        protected void RaiseOnStateChanged()
        {
            // raise the OnStateChanged event
            if (StateChanged == null)
            {
                return;
            }

            try
            {
                StateChanged(this, null);
            }
            catch (Exception ex)
            {
                TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, GetType().Name + ".RaiseOnStateChanged", ex);
            }
        }

        /// <summary>
        /// Performs the required state transition of the current BaseStateManagement instance.<br/>
        /// Calls the InternalConnect / InternalDisconnect virtual methods.
        /// </summary>
        /// <param name="reconnecting">Identifies whether this is a reconnecting call issued by the reconnect handler or not.</param>
        protected void PerformStateTransition(bool reconnecting)
        {
            bool performChildrenStateTransition = false;
            State currentState = CurrentState;
            State targetState = TargetState;

            lock (m_stateTransitionSync)
            {
                DisableReconnectHandler();

                if ((Parent == null) || (Parent != null && Parent.CurrentState != State.Disconnected))
                {
                    if (targetState != State.Disconnected)
                    {
                        InternalConnect(targetState, reconnecting);
                        CurrentState = targetState;
                        performChildrenStateTransition = true;
                    }
                    else if (targetState == State.Disconnected && currentState != State.Disconnected)
                    {
                        InternalDisconnect(reconnecting);
                        CurrentState = targetState;
                        performChildrenStateTransition = true;
                    }
                }
                else if (currentState != State.Disconnected)
                {
                    // only when parent is not null and disconnected
                    targetState = State.Disconnected;
                    InternalDisconnect(reconnecting);
                    CurrentState = targetState;
                    performChildrenStateTransition = true;
                }
            }

            if (currentState != CurrentState)
            {
                RaiseOnStateChanged();
            }

            if (performChildrenStateTransition)
            {
                PerformChildrenStateTransition(reconnecting);
            }
        }

        /// <summary>
        /// Iterates through all the BaseStateManagement children of the current BaseStateManagement instance and resolves their current state (performs the state transition).
        /// </summary>
        /// <param name="reconnecting">Identifies whether this is a reconnecting call issued by the reconnect handler or not.</param>
        protected void PerformChildrenStateTransition(bool reconnecting)
        {
            List<BaseStateManagement> children = GetChildren();

            if (children.Count == 0)
            {
                return;
            }

            foreach (var child in children)
            {
                try
                {
                    child.PerformStateTransition(reconnecting);
                }
                catch (Exception ex)
                {
                    TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "BaseStateManagement.PerformChildrenStateTransition", ex);
                }
            }
        }

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// Sets the children target state.<br/>
        /// This method is only called if connect/disconnect methods are called with deep=true.
        /// </summary>
        /// <param name="targetState">The target state the children BaseStateManagement instances must be set to.</param>
        private void SetChildrenTargetState(State targetState)
        {
            var children = GetChildren();

            if (children.Count == 0)
            {
                return;
            }

            foreach (var child in children)
            {
                try
                {
                    child.TargetState = targetState;
                    child.SetChildrenTargetState(targetState);
                }
                catch (Exception ex)
                {
                    TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "BaseStateManagement.PerformChildrenStateTransition", ex);
                }
            }
        }

        #endregion Private Methods
    }
}
