using System.Collections.Generic;

namespace BusinessObjects.Validators {
    /// <summary>
    /// Validates that all list items match the allowed length.
    /// </summary>
    public class ListOfStringLengthValidator : LengthValidator
    {

        private int? _offendingIndex;
        private string _description;

        /// <summary>
        /// Validates that a property value matches the allowed length.
        /// </summary>
        public ListOfStringLengthValidator(int length) : this(null, length) { }
        public ListOfStringLengthValidator(string propertyName, int length) : this(propertyName, string.Format("Length must be {0}", length), length) { }
        public ListOfStringLengthValidator(string propertyName, string description, int length) : this(propertyName, description, length, length) { }
        public ListOfStringLengthValidator(int min, int max) : this(null,  min, max) { }
        public ListOfStringLengthValidator(string propertyName, int min, int max) : this(propertyName, string.Format("Length must be between {0} and {1}", min, max), min, max) { }

        public ListOfStringLengthValidator(string propertyName, string description, int min, int max) : base(propertyName, description, min, max) { }

        public override bool Validate(BusinessObjectBase businessObject) {
            var v = (List<string>)GetPropertyValue(businessObject, PropertyName);
            for (var i = 0; i < v.Count; i++)
            {
                var s = v[i];
                if (string.IsNullOrEmpty(s) || (s.Length >= Min && s.Length <= Max)) continue;
                _offendingIndex = i;
                return false;
            }
            return true;
        }
        public override string Description {
            get
            {
                return (_offendingIndex == null)
                    ? _description
                    : string.Format("{0} (#{1}).", _description, _offendingIndex);
            }
            set { _description = value; } }
    }
}
