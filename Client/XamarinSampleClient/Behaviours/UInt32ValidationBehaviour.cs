/* ========================================================================
 * Copyright © 2011-2022 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
 * 
 * ======================================================================*/

using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;

namespace XamarinSampleClient.Behaviours
{
    /// <summary>
    /// Special behavior for Uint32 entries
    /// </summary>
    public class UInt32ValidationBehaviour : Behavior<Entry>
    {
        private static Dictionary<Entry, string> LastValidValue;

        static UInt32ValidationBehaviour()
        {
            LastValidValue = new Dictionary<Entry, string>();
        }
        protected override void OnAttachedTo(Entry entry)
        {
            entry.TextChanged += OnEntryTextChanged;
            LastValidValue[entry] = entry.Text;
            base.OnAttachedTo(entry);
        }

        protected override void OnDetachingFrom(Entry entry)
        {
            entry.TextChanged -= OnEntryTextChanged;
            LastValidValue.Remove(entry);
            base.OnDetachingFrom(entry);
        }

        private static void OnEntryTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!string.IsNullOrWhiteSpace(args.NewTextValue))
            {
                Entry entry = sender as Entry;
                if (entry != null)
                {
                    if (!LastValidValue.ContainsKey(entry))
                    {
                        LastValidValue[entry] = "0";
                    }
                    bool isValid = args.NewTextValue.ToCharArray().All(IsDigit); //Make sure all characters are numbers
                    if (isValid)
                    {
                        entry.Text = args.NewTextValue;
                        LastValidValue[entry] = args.NewTextValue;
                    }
                    else
                    {
                        entry.Text = LastValidValue[entry];
                    }
                }
            }
        }

        /// <summary>
        /// Check to see if a character is a digit.
        /// </summary>
        /// <param name="c">The character to check.</param>
        /// <returns><c>true</c> if the character is between <c>0</c> and <c>9</c>.</returns>
        private static bool IsDigit(char c)
        {
            if (c >= 48)
            {
                return c <= 57;
            }

            return false;
        }
    }

}
