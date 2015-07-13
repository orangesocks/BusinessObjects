using System.Collections.Generic;

namespace BusinessObjects.Tests
{
    public class ListOfBusinessObject : BusinessObject
    {
        private readonly List<ComplexObject> _list;

        public ListOfBusinessObject()
        {
            _list = new List<ComplexObject> {new ComplexObject(), new ComplexObject()};
        }

        [DataProperty]
        public List<ComplexObject> ListOfObjects { get {return _list;}}

    }
}