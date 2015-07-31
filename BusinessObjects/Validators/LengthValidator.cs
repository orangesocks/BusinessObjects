namespace BusinessObjects.Validators {
    /// <summary>
    /// Validates that a property value matches the allowed length.
    /// </summary>
    public class LengthValidator : Validator {

        protected readonly int Max;
        protected readonly int Min;

        /// <summary>
        /// Validates that a property value matches the allowed length.
        /// </summary>
        public LengthValidator(int length) : this(null, length) { }
        public LengthValidator(string propertyName, int length) : this(propertyName, string.Format("Length must be {0}.", length), length) { }
        public LengthValidator(string propertyName, string description, int length) : this(propertyName, description, length, length) { }
        public LengthValidator(int min, int max) : this(null,  min, max) { }
        public LengthValidator(string propertyName, int min, int max) : this(propertyName, string.Format("Length must be between {0} and {1}.", min, max), min, max) { }
        public LengthValidator(string propertyName, string description, int min, int max) : base(propertyName, description) {
            Max = max;
            Min = min;
        }

        public override bool Validate(BusinessObjectBase businessObject) {
            var v = (string)GetPropertyValue(businessObject, PropertyName);
            return string.IsNullOrEmpty(v) || v.Length >= Min && v.Length <= Max;
        }
    }
}
