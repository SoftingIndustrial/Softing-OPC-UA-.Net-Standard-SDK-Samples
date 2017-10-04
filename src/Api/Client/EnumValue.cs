using System;
using System.Collections.Generic;
using System.Text;

namespace Opc.Ua.Toolkit.Client
{/// <summary>
 /// 
 /// </summary>
    public class EnumValue : ComplexValue
    {
        /// <summary>
        /// Gets the name of the type.
        /// </summary>
        /// <value>
        /// The name of the type.
        /// </value>
        public string TypeName
        {
            get
            {
                return m_typeName;
            }
        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public int Value
        {
            get
            {
                return m_value;
            }
            set
            {
                m_value = value;

                if (m_enumFields != null)
                {
                    int i = 0;

                    for (; i < m_enumFields.Count; i++)
                    {
                        EnumField field = m_enumFields[i];

                        if (m_value == field.Value)
                        {
                            m_valueString = field.Name;
                            break;
                        }
                    }

                    if (i == m_enumFields.Count)
                    {
                        m_valueString = string.Empty;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the value string.
        /// </summary>
        /// <value>
        /// The value string.
        /// </value>
        public string ValueString
        {
            get
            {
                return m_valueString;
            }
            set
            {
                m_valueString = value;

                if (m_enumFields != null)
                {
                    int i = 0;

                    for (; i < m_enumFields.Count; i++)
                    {
                        EnumField field = m_enumFields[i];

                        if (value == field.Name)
                        {
                            m_valueString = value;
                            m_value = field.Value;
                            break;
                        }
                    }

                    if (i == m_enumFields.Count)
                    {
                        m_value = 0;
                        m_valueString = string.Empty;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the value strings.
        /// </summary>
        public IEnumerable<string> ValueStrings
        {
            get
            {
                if (m_enumFields != null)
                {
                    List<string> values = new List<string>(m_enumFields.Count);
                    foreach (var field in m_enumFields)
                    {
                        values.Add(string.Format("{0} ({1})", field.Value, field.Name));
                    }
                    return values;
                }

                return null;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumValue"/> class.
        /// </summary>
        public EnumValue()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumValue"/> class.
        /// </summary>
        public EnumValue(string typeName, int value, string valueString)
        {
            m_typeName = typeName;
            m_value = value;
            m_valueString = valueString;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StructuredValue"/> class.
        /// </summary>
        /// <param name="typeId">The type id.</param>
        /// <param name="factory">The factory.</param>
        public EnumValue(ExpandedNodeId typeId, EncodeableFactory factory)
        {
            Schema.Binary.EnumeratedType enumeratedType = factory.GetEnumeratedType(typeId);
            if (enumeratedType == null)
            {
                throw new NotSupportedException("The typeId does not match a known Enumerated type");
            }

            Initialize(enumeratedType);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StructuredValue"/> class.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="factory">The factory.</param>
        public EnumValue(System.Xml.XmlQualifiedName typeName, EncodeableFactory factory)
        {
            Schema.Binary.EnumeratedType enumeratedType = factory.GetEnumeratedType(typeName);
            if (enumeratedType == null)
            {
                throw new NotSupportedException("The typeName does not match a known Enumerated type");
            }

            Initialize(enumeratedType);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StructuredValue"/> class.
        /// </summary>
        /// <param name="enumeratedType">Type of the structured.</param>
        public EnumValue(Schema.Binary.EnumeratedType enumeratedType)
        {
            Initialize(enumeratedType);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumValue"/> class.
        /// </summary>
        /// <param name="enumType">Type of the enum.</param>
        public EnumValue(Type enumType)
        {
            if (enumType.IsEnum)
            {
                m_typeName = enumType.Name;

                string[] enumNames = Enum.GetNames(enumType);
                Array enumValues = Enum.GetValues(enumType);
                m_enumFields = new List<EnumField>(enumNames.Length);

                if (enumNames.Length > 0)
                {
                    for (int i = 0; i < enumNames.Length; i++)
                    {
                        m_enumFields.Add(new EnumField(enumNames[i], (int)enumValues.GetValue(i)));
                    }

                    m_value = (int)enumValues.GetValue(0);
                    m_valueString = enumNames[0];
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StructuredValue"/> class.
        /// </summary>
        /// <param name="enumeratedType">Type of the structured.</param>
        private void Initialize(Schema.Binary.EnumeratedType enumeratedType)
        {
            m_typeName = enumeratedType.Name;
            m_enumFields = new List<EnumField>(enumeratedType.EnumeratedValue.Length);

            if (enumeratedType.EnumeratedValue.Length > 0)
            {
                foreach (var field in enumeratedType.EnumeratedValue)
                {
                    m_enumFields.Add(new EnumField(field.Name, field.Value));
                }

                m_value = enumeratedType.EnumeratedValue[0].Value;
                m_valueString = enumeratedType.EnumeratedValue[0].Name;
            }
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            return this.MemberwiseClone();
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj is int)
            {
                return Value == (int)obj;
            }

            EnumValue enumValue = obj as EnumValue;
            if (enumValue == null)
            {
                return false;
            }

            return Value == enumValue.Value;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return m_valueString;
        }

        #region Private fields
        int m_value;
        string m_valueString;
        List<EnumField> m_enumFields;
        string m_typeName;
        #endregion
    }
}
