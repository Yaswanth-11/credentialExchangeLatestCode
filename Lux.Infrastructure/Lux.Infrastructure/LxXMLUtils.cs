using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Lux.Infrastructure
{
    class LxXMLUtils
    {

        private const string INVALID_XML_FORMAT_MESSAGE =
            "Invalid XML format";
        public static void LxXMLElementTagNameEquals(XElement element,
                string expectedTagName)
        {
            string tagName = element != null ? element.Name.LocalName : null;
            if (tagName == null || !tagName.Equals(expectedTagName))
            {
                throw new LxException("Invalid xml data" +
                        " - expected tag name '" + expectedTagName + "' but was '" + tagName + "'",
                        LxErrorCodes.E_INVALID_XML_FORMAT);
            }
        }
        public static XDocument LxXMLParse(string xmlString)
        {
            XDocument document = null;

            LxAssert.NotNullOrEmpty(xmlString, "xmlString");

            try
            {
                using (TextReader reader = new StringReader(xmlString))
                {
                    document = XDocument.Load(reader);

                    if (null == document)
                    {
                        throw new LxException(LxErrorCodes.E_INVALID_XML_FORMAT);
                    }
                }
            }
            catch (System.Xml.XmlException)
            {
                throw new LxException(LxErrorCodes.E_INVALID_XML_FORMAT);
            }

            return document;
        }
        public static void LxXMLElementNotNull(XElement element, string elementName)
        {
            if (element == null)
            {
                throw new LxException(INVALID_XML_FORMAT_MESSAGE +
                        " - '" + elementName + "' element must not be null",
                        LxErrorCodes.E_INVALID_XML_FORMAT);
            }
        }
        public static void LxXMLElementNotNullOrEmpty(string value, string name)
        {
            if (String.IsNullOrEmpty(value))
            {
                throw new LxException(INVALID_XML_FORMAT_MESSAGE +
                        " - '" + name + "' must not be null or empty",
                        LxErrorCodes.E_INVALID_XML_FORMAT);
            }
        }

        public static XElement LxXMLGetFirstChildElement(XElement parent, string elementName)
        {
            if (null != parent)
            {
                foreach (XNode node in parent.DescendantNodes())
                {
                    XElement xElement = node as XElement;
                    if (null != xElement)
                    {
                        if (xElement.Name.LocalName.Equals(elementName))
                            return xElement;
                    }
                }
            }
            return null;
        }

        public static List<XElement> LxXMLGetChildElements(XElement responseDataElement)
        {
            List<XElement> childElements = new List<XElement>();

            if (responseDataElement != null)
            {
                var childNodes = responseDataElement.Nodes();
                foreach (XNode xNode in childNodes)
                {
                    XElement xElement = xNode as XElement;
                    if (null != xElement)
                        childElements.Add((xElement));
                }
            }

            return childElements;
        }

        public static string LXXMLGetTextContextOfFirstChildElement(XElement element,
            string tagName)
        {
            XElement firstChildElement = LxXMLGetFirstChildElement(element, tagName);
            if (null == firstChildElement)
                return null;

            return firstChildElement.Value;
        }

        public static int LxXMLGetTextContextOfFirstChildElementAsInt32(XElement element,
            string tagName)
        {
            XElement firstChildElement = LxXMLGetFirstChildElement(element, tagName);
            if (firstChildElement == null)
            {
                return -1;
            }
            return Convert.ToInt32(firstChildElement.Value, CultureInfo.InvariantCulture);
        }

        public static int LxXMLGetTextContextAsInt32(XElement element)
        {
            if (element == null || String.IsNullOrEmpty(element.Value))
            {
                return -1;
            }

            return Convert.ToInt32(element.Value, CultureInfo.InvariantCulture);
        }

        public static T ParseEnum<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }
    }
}
