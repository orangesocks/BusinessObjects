using BusinessObjects.Validators;
using NUnit.Framework;

namespace BusinessObjects.Tests
{
    [TestFixture]
    public class BusinessObjectBaseTests : BaseClass
    {
        [Test]
        public void ListOfObjectsPrintsIndexOfInvalidItem() {
            var o = new ListOfBusinessObject();
            Assert.IsFalse(o.IsValid);
            Assert.IsTrue(o.Error.Contains("[0]"));
            Assert.IsTrue(o.Error.Contains("[1]"));
        }
        [Test]
        public void IsEmpty() {
            var o = new SimpleObject();
            Assert.IsTrue(o.IsEmpty());

            o.SimpleProperty = "hello";
            Assert.IsFalse(o.IsEmpty());
        }

        [Test]
        public void NonExistingProperty() {
            var o = GetMock();
            Assert.AreEqual(o.GetBrokenRules("non_existing_property").Count, 0);
            Assert.IsNull(o["non_existing_property"]);
        }

        #region V A L I D A T O R S

        [Test]
        public void RequiredValidator() {

            const string propertyName = "RequiredProperty";

            // instantiate an invalid object (required property is null).
            var o = new ComplexObject();
            AssertInvalidObject(o, propertyName, typeof(RequiredValidator));

            o.RequiredProperty = "hello";
            AssertValidObject(o, propertyName);
        }

        [Test]
        public void LengthValidator() {
            const string propertyName = "LengthProperty";
            var expectedValidatorType = typeof (LengthValidator);

            var o = GetMock();
            o.LengthProperty = "too long for ya";
            AssertInvalidObject(o, propertyName, expectedValidatorType);

            o.LengthProperty = "hello";
            AssertValidObject(o, propertyName);
        }

        [Test]
        public void ListOfStringLengthValidator() {
            const string propertyName = "ListOfStringsProperty";
            var expectedValidatorType = typeof (ListOfStringLengthValidator);

            var o = GetMock();
            o.ListOfStringsProperty.Add("too long for ya");
            AssertInvalidObject(o, propertyName, expectedValidatorType);
            Assert.IsTrue(o.Error.Contains("#0"));

            o.ListOfStringsProperty.Clear();
            o.ListOfStringsProperty.Add("hello");
            o.ListOfStringsProperty.Add("too long for ya");
            AssertInvalidObject(o, propertyName, expectedValidatorType);
            Assert.IsTrue(o.Error.Contains("#1"));

            o.ListOfStringsProperty.Clear();
            o.ListOfStringsProperty.Add("");
            o.ListOfStringsProperty.Add("hello");
            AssertValidObject(o, propertyName);
        }

        [Test]
        public void CountryValidator() {

            const string propertyName = "CountryProperty";

            var o = GetMock();

            o.CountryProperty = "Don't think so";
            AssertInvalidObject(o, propertyName, typeof(CountryValidator));

            // Empty country is valid.
            o.CountryProperty = "";
            AssertValidObject(o, propertyName);

            o.CountryProperty = "IT";
            AssertValidObject(o, propertyName);
        }

        [Test]
        public void DelegateValidator() {

            const string propertyName = "DelegateProperty";

            var o = GetMock();

            o.DelegateProperty = "Don't think so";
            AssertInvalidObject(o, propertyName, typeof(DelegateValidator));

            o.DelegateProperty = "dummy";
            AssertValidObject(o, propertyName);
        }

        [Test]
        public void RegexValidator() {

            const string propertyName = "RegexProperty";

            var o = GetMock();

            o.RegexProperty = "Don't think so";
            AssertInvalidObject(o, propertyName, typeof(RegexValidator));

            o.RegexProperty = "dummy";
            AssertValidObject(o, propertyName);
        }

        [Test]
        public void XorRequiredValidator() {

            const string propertyName = null;

            var o = GetMock();

            o.FirstProperty = null;
            o.SecondProperty = null;
            AssertInvalidObject(o, propertyName, typeof(XorRequiredValidator));

            o.SecondProperty = "whatever";
            AssertValidObject(o, propertyName);
        }

        [Test]
        public void AndCompositeValidator() {

            const string propertyName = "AndProperty";

            var o = GetMock();

            o.AndProperty = null;
            AssertInvalidObject(o, propertyName, typeof(AndCompositeValidator));

            o.AndProperty = "And";
            AssertValidObject(o, propertyName);
        }
        #endregion
    }
}
