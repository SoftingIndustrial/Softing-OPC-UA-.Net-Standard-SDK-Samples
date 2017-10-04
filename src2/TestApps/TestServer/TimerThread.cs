using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace TestServer
{
	/// <summary>
	/// This class implements an action scheduler. Actions are scheduled by the time when they
	/// need to be triggered/executed.
	/// </summary>
	class TimerThread
	{
		LinkedList<ScheduledAction> m_actions;
		BackgroundWorker m_worker;

		public TimerThread()
		{
			m_worker = new BackgroundWorker();
			m_worker.DoWork += new DoWorkEventHandler(m_worker_DoWork);
			m_worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(m_worker_RunWorkerCompleted);
			m_worker.WorkerSupportsCancellation = true;

			m_actions = new LinkedList<ScheduledAction>();
		}

		void m_worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			
		}

		void m_worker_DoWork(object sender, DoWorkEventArgs e)
		{
			List<LinkedListNode<ScheduledAction>> triggeredActions = new List<LinkedListNode<ScheduledAction>>();
			//const int defaultWaitingTime = 0;

			while(!m_worker.CancellationPending)
			{
				triggeredActions.Clear();
				int sleepingTime = 0;
				//bool isSleepingTimeSet = false;

				lock(m_actions)
				{
					// We trigger all the actions that are expired

					while(sleepingTime == 0 && !m_worker.CancellationPending && m_actions.Count > 0)
					{
						ScheduledAction action = m_actions.First.Value;

						sleepingTime = action.TimeToTrigger;
						//isSleepingTimeSet = true;

						if (sleepingTime == 0)
						{
							// we firstly update the lists  because te user might
							// try to remove the action from within the execute
							triggeredActions.Add(m_actions.First);
							m_actions.RemoveFirst();

							try
							{
								action.Execute();
							}
							catch
							{
							}
						}
					}

					if (triggeredActions.Count > 0)
					{
						// Insert all the triggered actions into the queue
						foreach(LinkedListNode<ScheduledAction> node in triggeredActions)
						{
							// Only cyclic actions are rescheduled
							if (node.Value.IsCyclic)
							{
								node.Value.Reschedule();
								AddAction(node);
							}
						}

						// Get the sleeping time to the next action to be triggered
						if (m_actions.Count > 0)
						{
							sleepingTime = m_actions.First.Value.TimeToTrigger;
							//isSleepingTimeSet = true;
						}
					}
				}

				//if (!isSleepingTimeSet)
				//{
				//    sleepingTime = defaultWaitingTime;
				//}

				if (sleepingTime > 0)
				{
					Thread.Sleep(sleepingTime);
				}
			}
		}

		bool AddAction(LinkedListNode<ScheduledAction> node)
		{
			ScheduledAction action = node.Value;
			LinkedListNode<ScheduledAction> n;
			lock (m_actions)
			{
				for (n = m_actions.First; n != null; n = n.Next)
				{
                    if (action < n.Value)
                    {
                        break;
                    }
				}

                if (n == null)
                {
                    m_actions.AddLast(node);
                }
                else
                {
                    m_actions.AddBefore(n, node);
                }
			}

			return true;
		}

		public bool AddAction(ScheduledAction action)
		{
			return AddAction(new LinkedListNode<ScheduledAction>(action));
		}

		public bool RemoveAction(ScheduledAction action)
		{
			lock(m_actions)
			{
				return m_actions.Remove(action);
			}
		}

		public void Start()
		{
			m_worker.RunWorkerAsync();
		}

		public void Stop()
		{
			m_worker.CancelAsync();
		}
	}
}