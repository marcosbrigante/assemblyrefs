using System;
using System.Collections.Generic;
using System.Xml;

namespace AssemblyRefs
{
    public class ResultWriter
    {
        #region Constants
        private const string ASSEMBLY_NODE = "assembly";
        private const string ROOT_NODE = "result";
        private const string NAME_NODE = "name";
        private const string FOUND_NODE = "found";
        private const string DEPENDS_NODE = "dependson";
        private const string REFERENCED_NODE = "referencedby";
        private const string BUILDTYPE_NODE = "buildtype";
        private const string DATE_ATTRIBUTE = "date";
        private const string DATE_ATTRIBUTE_FORMAT = "dd/MM/yyyy HH:mm:ss";
        private const string TARGET_ATTRIBUTE = "target";
        #endregion

        private XmlDocument _doc = new XmlDocument();
        private AssemblyModel _currentTarget = null;

        public void Write(AssemblyModel result, string fileName)
        {
            if (result == null)
                throw new ArgumentNullException("result", "The analysis result should not be null");

            CreateXml(result);

            var settings = new XmlWriterSettings();
            var writer = new XmlTextWriter(fileName, null)
            {
                Formatting = Formatting.Indented
            };

            try
            {
                _doc.WriteTo(writer);
                writer.Flush();
            }
            finally
            {
                writer.Close();
            }
        }

        private void CreateXml(AssemblyModel model)
        {
            var dic = new Dictionary<string, XmlNode>();
            _currentTarget = model;

            #region Declaration
            _doc.AppendChild(_doc.CreateNode(XmlNodeType.XmlDeclaration, string.Empty, string.Empty));
            #endregion

            #region Root
            var root = _doc.CreateNode(XmlNodeType.Element, ROOT_NODE, string.Empty);
            var dateAttribute = _doc.CreateAttribute(DATE_ATTRIBUTE);

            dateAttribute.Value = DateTime.Now.ToString(DATE_ATTRIBUTE_FORMAT);

            root.Attributes.Append(dateAttribute);
            #endregion

            foreach (var cachedModel in AssemblyModel.AssemblyModelCache)
            {
                var node = CreateAssemblyModelNode(cachedModel, ASSEMBLY_NODE);

                #region Dependencies
                var referencesNode = _doc.CreateNode(XmlNodeType.Element, DEPENDS_NODE, null);

                foreach (var reference in cachedModel.References)
                {
                    referencesNode.AppendChild(CreateAssemblyModelNode(reference, ASSEMBLY_NODE));
                }

                node.AppendChild(referencesNode);
                #endregion

                #region Referenced By Nodes
                var referencedBy = _doc.CreateNode(XmlNodeType.Element, REFERENCED_NODE, null);
                node.AppendChild(referencedBy);

                dic.Add(cachedModel.Name, referencedBy);
                #endregion

                root.AppendChild(node);
            }

            foreach (var cachedModel in AssemblyModel.AssemblyModelCache)
            {
                foreach (var reference in cachedModel.References)
                {
                    dic[reference.Name].AppendChild(CreateAssemblyModelNode(cachedModel, ASSEMBLY_NODE));
                }
            }

            _doc.AppendChild(root);
        }

        private XmlNode CreateAssemblyModelNode(AssemblyModel model, string nodeName)
        {
            var node = _doc.CreateNode(XmlNodeType.Element, nodeName, null);
            var name = _doc.CreateNode(XmlNodeType.Element, NAME_NODE, null);
            var found = _doc.CreateNode(XmlNodeType.Element, FOUND_NODE, null);
            var buildType = _doc.CreateNode(XmlNodeType.Element, BUILDTYPE_NODE, null);

            name.InnerXml = model.Name;
            found.InnerXml = model.Found.ToString();
            buildType.InnerXml = model.BuildType.ToString();

            node.AppendChild(name);
            node.AppendChild(found);
            node.AppendChild(buildType);

            if (model.Equals(_currentTarget))
            {
                var targetAttribute = _doc.CreateAttribute(TARGET_ATTRIBUTE);

                targetAttribute.Value = true.ToString();
                node.Attributes.Append(targetAttribute);
            }

            return node;
        }
    }
}
