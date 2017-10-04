using System;

namespace TestServer
{
	/// <summary>
	/// Class representing an action that might be scheduled by the TimerThread.
	/// The implementing class must overload the execute() method with an appropriate implementation.
	/// It is also possible to add a priority parameter to the action. In order to achieve this
	/// this class should be extended with a priority parameter, and the comparison operators
	/// must be adjusted.
	/// </summary>
	abstract class ScheduledAction
	{
		int m_startTime;	// Time when the action has been created.
		int m_endTime;		// The time when this action needs to be triggered.
		bool m_isCyclic;	// True if this action must be scheduled over and over again.

		/// <summary>
		/// Default constructor initializes start and end time to the current time..
		/// </summary>
		/// <param name="isCyclic"></param>
		public ScheduledAction(bool isCyclic)
		{
			m_isCyclic = isCyclic;
			m_startTime = m_endTime = (int) (DateTime.Now.Ticks / 10000);
		}

		/// <summary>
		/// Construction of an ScheduledAction object, given a specified timeout. The starting point 
		/// for the countdown will be considered to be the current tick count.
		/// </summary>
		/// <param name="timeout"></param>
		/// <param name="isCyclic"></param>
		public ScheduledAction(int timeout, bool isCyclic)
		{
			m_isCyclic = isCyclic;
			m_startTime = (int) (DateTime.Now.Ticks / 10000);
			m_endTime = m_startTime + timeout;
		}

		/// <summary>
		/// Construction of an ScheduledAction object, given a start and end time.
		/// startTime must not be in the future, it must either equal the current 'tick count' or lie in the past.
		/// </summary>
		/// <param name="startTime"></param>
		/// <param name="endTime"></param>
		/// <param name="isCyclic"></param>
		//public ScheduledAction(int startTime, int endTime, bool isCyclic)
		//{
		//    m_isCyclic = isCyclic;
		//    m_startTime = startTime;
		//    m_endTime = endTime;
		//}

		/// <summary>
		/// The execute() method should be a 'short and fast' implementation. Delays that occur in the 
		/// execute method can cause the delay of execution of following actions.
		/// </summary>
		public abstract void Execute();

		public int TimeToTrigger
		{
			get
			{
				int currentTick = (int) (DateTime.Now.Ticks / 10000);

				// First check the case when no overflow occurs for this action
				if (m_startTime <= m_endTime)
				{
                    if (m_startTime <= currentTick && currentTick <= m_endTime)
                    {
                        return m_endTime - currentTick;
                    }
				}
				// Now check the case with overflow
				else
				{
                    if (currentTick >= m_startTime)
                    {
                        return int.MaxValue - currentTick + m_endTime;
                    }
                    else if (currentTick <= m_endTime)
                    {
                        return m_endTime - currentTick;
                    }
				}

				return 0;
			}
		}

		public bool IsCyclic
		{
			get { return m_isCyclic; }
			set { m_isCyclic = value; }
		}

		public int Timeout
		{
			set
			{
				m_startTime = (int) (DateTime.Now.Ticks / 10000);
				m_endTime = m_startTime + value;
			}
		}

		/// <summary>
		/// Recalculate the start and end time. The start will be considered the current time.
		/// This method should be called after triggering an action and before rescheduling the action.
		/// </summary>
		public void Reschedule()
		{
			int timeout = m_endTime - m_startTime;
			m_startTime = m_endTime;
			m_endTime = m_startTime + timeout;
		}

		/// <summary>
		/// Comparison operator of two action objects. Elements are compared by the scheduled  
		/// time. Overflow of 'tick count' is payed attention to.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		static public bool operator <(ScheduledAction left, ScheduledAction right)
		{
			// If no overflow occurs on both actions, or an overflow occurs on both actions ('this' and 'right')
			if ((left.m_startTime <= left.m_endTime && right.m_startTime <= right.m_endTime) ||
			   (left.m_startTime >= left.m_endTime && right.m_startTime >= right.m_endTime))
			{
				return left.m_endTime < right.m_endTime;
			}
			// If a time tick overflow occurs only on the 'this' object
			else if (left.m_startTime > left.m_endTime && right.m_startTime < right.m_endTime)
			{
				return false;
			}
			// If a time tick overflow occurs on 'right' object
			else if (left.m_startTime < left.m_endTime && right.m_startTime > right.m_endTime)
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// Comparison operator of two action objects. Elements are compared by the scheduled  
		/// time. Overflow of 'tick count' is payed attention to.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		static public bool operator <=(ScheduledAction left, ScheduledAction right)
		{
			// If no overflow occurs on both actions, or an overflow occurs on both actions ('this' and 'right')
			if ((left.m_startTime <= left.m_endTime && right.m_startTime <= right.m_endTime) ||
			   (left.m_startTime >= left.m_endTime && right.m_startTime >= right.m_endTime))
			{
				return left.m_endTime <= right.m_endTime;
			}
			// If a time tick overflow occurs only on the 'this' object
			else if (left.m_startTime > left.m_endTime && right.m_startTime < right.m_endTime)
			{
				return false;
			}
			// If a time tick overflow occurs on 'right' object
			else if (left.m_startTime < left.m_endTime && right.m_startTime > right.m_endTime)
			{
				return true;
			}

			return false;
		}

		static public bool operator >(ScheduledAction left, ScheduledAction right)
		{
			return !(left <= right);
		}

		static public bool operator >=(ScheduledAction left, ScheduledAction right)
		{
			return !(left < right);
		}
	}
}