namespace TestServer.TestModule
{
	class SimVarChangeAction : ScheduledAction
	{
		SimVarSet m_varSet;
		uint m_changeInterval;
		uint m_repeatCount;
		uint m_changeCount;
		double m_increment;
		int m_lastIndex;
		public int Total { get; set; }

		public SimVarChangeAction(SimVarSet varSet, uint changeInterval, uint repeatCount,
			double increment, uint changeCount)
			: base(1000, true)
		{
			m_varSet = varSet;
			m_changeInterval = changeInterval;
			m_repeatCount = repeatCount;
			m_increment = increment;
			m_changeCount = changeCount;
			Timeout = (int) changeInterval;
			IsCyclic = repeatCount >= 0;
			Total = 0;
		}

		public override void Execute()
		{
			int size = m_varSet.Count;
			int changed;

			if (m_changeCount != 0) // we perform the given number of changes and make a wrap around
			{
				uint changes = m_changeCount;

				while(changes > 0)
				{
					if (m_lastIndex >= size)
					{
						m_lastIndex = 0;
					}

					m_varSet[m_lastIndex++].IncrementValue(m_increment);
					changes--;
				}

				changed = (int) m_changeCount;
			}
			else // make change on comlete set of variables in each cycle
			{
				for(int i = 0; i < size; i++)
				{
					m_varSet[i].IncrementValue(m_increment);
				}

				changed = size;
			}
            
			//double duration = (DateTime.Now - startTime).TotalMilliseconds;
			//if (duration > m_changeInterval)
				//Console.WriteLine("Changed {0} values in {1}ms.", changed, duration);

			Total += changed;

			if (m_repeatCount > 0)
			{
				m_repeatCount--;

				if (m_repeatCount == 0)
				{
					// We reached the end of repeat count, remove the action from the scheduler
					IsCyclic = false;
				}
			}
		}
	}
}