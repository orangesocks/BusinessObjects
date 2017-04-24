﻿using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace BusinessObjects
{
    /// <summary>
    /// - XML (de)serialization;
    /// - JSON serialization.
    /// </summary>
    public class BusinessObject : 
        BusinessObjectBase,
        IXmlSerializable {
        /// <summary>
        /// Constructor.
        /// </summary>
        protected BusinessObject() { 
            XmlOptions = new XmlOptions();
        }
        protected BusinessObject(XmlReader r) : this() { ReadXml(r); }

        public XmlOptions XmlOptions { get; set; }

        public bool ShouldSerializeIsValid() { return false; }
        public bool ShouldSerializeError() { return false; }
        public bool ShouldSerializeIsEmpty() { return false; }
        public bool ShouldSerializeXmlDateFormat() { return false; }

        /// <summary>
        /// Serializes the instance to JSON
        /// </summary>
        /// <returns>A JSON string representing the class instance.</returns>
        public virtual string ToJson() {
            return ToJson(JsonOptions.None);
        }
        /// <summary>
        /// Serializes the class to JSON.
        /// </summary>
        /// <param name="jsonOptions">JSON formatting options.</param>
        /// <returns>A JSON string representing the class instance.</returns>
        public virtual string ToJson(JsonOptions jsonOptions) {
            var json = JsonConvert.SerializeObject(this, 
                (jsonOptions == JsonOptions.Indented) ? Formatting.Indented : Formatting.None,
                new JsonSerializerSettings { 
                    ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                    //NullValueHandling = NullValueHandling.Ignore,
                });
            return json;
        }
        #region XML

        public XmlSchema GetSchema() { return null; }

        /// <summary>
        /// Serializes the current BusinessObject instance to a XML file.
        /// </summary>
        /// <param name="fileName">Name of the file to write to.</param>
        public virtual void WriteXml(string fileName) {
            var settings = new XmlWriterSettings {Indent = true};
            using (var writer = XmlWriter.Create(new System.Text.StringBuilder(fileName), settings)) { WriteXml(writer); }
        }

        /// <summary>
        /// Serializes the current BusinessObject instance to a XML stream.
        /// </summary>
        /// <param name="w">Active XML stream writer.</param>
        /// <remarks>Writes only its inner content, not the outer element. Leaves the writer at the same depth.</remarks>
        public virtual void WriteXml(XmlWriter w) {
            foreach (var prop in GetAllDataProperties())
            {
                var propertyValue = prop.GetValue(this, null);
                if (propertyValue == null && !XmlOptions.SerializeNullValues) continue;

                // if it's a BusinessObject instance just let it flush it's own data.
                var child = propertyValue as BusinessObject;
                if (child != null) {
                    if (child.IsEmpty() && XmlOptions.SerializeEmptyBusinessObjects == false) continue;
                    w.WriteStartElement((string.IsNullOrEmpty(child.XmlOptions.ElementName) ?  child.GetType().Name : child.XmlOptions.ElementName));
                    child.WriteXml(w);
                    w.WriteEndElement();
                    continue;
                }

                // if property type is List<T>, assume it's of BusinessObjects and try to fetch them all from XML.
                var tList = typeof (List<>);
                var propertyType = prop.PropertyType;
                if (prop.PropertyType.IsGenericType && tList.IsAssignableFrom(propertyType.GetGenericTypeDefinition()) ||
                    propertyType.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == tList)) {
                    WriteXmlList(prop.Name, propertyValue, w);
                    continue;
                }

                if (propertyValue is string) {
                    if (!string.IsNullOrEmpty(propertyValue.ToString()) || XmlOptions.SerializeEmptyStrings) {
                        w.WriteElementString(prop.Name, propertyValue.ToString());
                    }
                    continue;
                }
                if (propertyValue is DateTime && XmlOptions.DateTimeFormat != null && !Attribute.IsDefined(prop, typeof(IgnoreXmlDateFormat))) {
                    w.WriteElementString(prop.Name, ((DateTime)propertyValue).ToString(XmlOptions.DateTimeFormat));
                    continue;
                }
                if (propertyValue is decimal && XmlOptions.DecimalFormat != null) {
                    w.WriteElementString(prop.Name, ((decimal)propertyValue).ToString(XmlOptions.DecimalFormat, CultureInfo.InvariantCulture));
                    continue;
                }

                // all else fail so just let the value flush straight to XML.
                w.WriteStartElement(prop.Name);
                if (propertyValue != null) { 
                    w.WriteValue(propertyValue); 
                }
                w.WriteEndElement();
            }
        }

        /// <summary>
        /// Deserializes a List of BusinessObject or strings to one or more XML elements.
        /// </summary>
        /// <param name="propertyName">Property name.</param>
        /// <param name="propertyValue">Property value.</param>
        /// <param name="w">Active XML stream writer.</param>
        private static void WriteXmlList(string propertyName, object propertyValue, XmlWriter w)
        {
            var e = propertyValue.GetType().GetMethod("GetEnumerator").Invoke(propertyValue, null) as IEnumerator;

            while (e != null && e.MoveNext()) {
                if (e.Current == null) continue;
                var current = e.Current;
                w.WriteStartElement(propertyName);
                {
                    if (current is BusinessObject) {
                        ((BusinessObject)current).WriteXml(w);
                    }
                    else if (current is string)
                    {
                        w.WriteString((string) current);
                    }
                    else {
                        w.WriteValue(e.Current);
                    }
                }
                w.WriteEndElement();
            }
        }

        /// <summary>
        /// Deserializes the current BusinessObject from a XML stream.
        /// </summary>
        /// <param name="r">Active XML stream reader.</param>
        /// <remarks>Reads the outer element. Leaves the reader at the same depth.</remarks>
        // TODO Clear properties before reading from file
        public virtual void ReadXml(XmlReader r) {
            var isEmpty = r.IsEmptyElement;
            
            r.ReadStartElement();
            if (isEmpty) return;

            var props = GetAllDataProperties().ToList();
            while (r.NodeType == XmlNodeType.Element) {

                var prop = props.FirstOrDefault(n => n.Name.Equals(r.Name));
                if (prop == null) {
                    // ignore unknown property.
                    r.Skip();
                    continue;
                }

                var propertyType = prop.PropertyType;
                var propertyValue = prop.GetValue(this, null);

                // if property type is BusinessObject, let it auto-load from XML.
                if (typeof(BusinessObject).IsAssignableFrom(propertyType)) {
                    ((BusinessObject)propertyValue).ReadXml(r);
                    continue;
                }

                // if property type is List<T>, try to fetch the list from XML.
                if (IsListOfT(propertyType)){
                    ReadXmlList(propertyValue, prop.Name, r);
                    continue;
                }

                // ReadElementContentAs won't accept a nullable types.
                if (typeof(DateTime?).IsAssignableFrom(propertyType)) {
                    propertyType = typeof(DateTime);
                }
                if (typeof(decimal?).IsAssignableFrom(propertyType)) {
                    propertyType = typeof(decimal);
                }
                if (typeof(int?).IsAssignableFrom(propertyType)) {
                    propertyType = typeof(int);
                }
                // ReSharper disable once AssignNullToNotNullAttribute
                prop.SetValue(this, r.ReadElementContentAs(propertyType, null), null);
            }
            r.ReadEndElement();
        }

        /// <summary>
        /// Serializes one or more XML elements into a List of BusinessObjects.
        /// </summary>
        /// <param name="propertyValue">Property value. Must be a List of BusinessObject instances.</param>
        /// <param name="propertyName">Property name.</param>
        /// <param name="r">Active XML stream reader.</param>
        private static void ReadXmlList(object propertyValue, string propertyName, XmlReader r) {

            // retrieve type of list elements.
            var elementType = propertyValue.GetType().GetGenericArguments().Single();

            // quit if it's not a BusinessObject subclass.
            if (elementType.BaseType == null) return;
            
            // clear the list first.
            propertyValue.GetType().GetMethod("Clear").Invoke(propertyValue, null);

            while (r.NodeType == XmlNodeType.Element && r.Name == propertyName) {
                if (typeof (BusinessObjectBase).IsAssignableFrom(elementType)) {
                    // list items are expected to be of BusinessObject type.
                    var bo = Activator.CreateInstance(elementType);
                    ((BusinessObject)bo).ReadXml(r);
                    propertyValue.GetType().GetMethod("Add").Invoke(propertyValue, new[] { bo });
                    continue;
                }
                if (elementType == typeof (string)) {
                    propertyValue.GetType().GetMethod("Add").Invoke(propertyValue, new object[] { r.ReadElementContentAsString() });
                }
                if (elementType == typeof (int)) {
                    propertyValue.GetType().GetMethod("Add").Invoke(propertyValue, new[] { r.ReadElementContentAs(elementType, null) });

                }
            }
        }
        #endregion
    }
}
