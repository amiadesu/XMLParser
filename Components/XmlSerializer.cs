using System.Collections.Generic;
using System.Text;
using System.Xml;
using XMLParser.Models;

namespace XMLParser.Services.Serialization
{
    public static class StudentXmlSerializer
    {
        public static string Serialize(IList<StudentModel> students)
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = Encoding.UTF8,
                OmitXmlDeclaration = false,
                NewLineChars = "\n"
            };

            var sb = new StringBuilder();
            using (var w = XmlWriter.Create(sb, settings))
            {
                w.WriteStartDocument();
                w.WriteStartElement("students");

                foreach (var s in students)
                {
                    WriteStudent(w, s);
                }

                w.WriteEndElement(); // </students>
                w.WriteEndDocument();
                w.Flush();
            }

            return sb.ToString();
        }

        private static void WriteStudent(XmlWriter w, StudentModel student)
        {
            w.WriteStartElement("student");

            WriteAttr(w, "id", student.Attributes.TryGetValue("id", out var id) ? id : null);
            WriteAttr(w, "group", student.Attributes.TryGetValue("group", out var group) ? group : null);
            WriteAttr(w, "year", student.Attributes.TryGetValue("year", out var year) ? year : null);
            WriteAttr(w, "dorm", student.Attributes.TryGetValue("dorm", out var dorm) ? dorm : null);

            WriteElem(w, "fullname", student.FullName);
            WriteElem(w, "faculty", student.Faculty);
            WriteElem(w, "department", student.Department);
            WriteElem(w, "specialty", student.Specialty);
            WriteElem(w, "eventWindow", student.EventWindow);
            WriteElem(w, "parliamentType", student.ParliamentType);

            if (student.Attributes is not null)
            {
                foreach (var kv in student.Attributes)
                {
                    if (kv.Key is "id" or "group" or "year" or "dorm"
                        or "fullname" or "faculty" or "department"
                        or "specialty" or "eventWindow" or "parliamentType")
                        continue;

                    WriteElem(w, kv.Key, kv.Value);
                }
            }

            w.WriteEndElement(); // </student>
        }

        private static void WriteAttr(XmlWriter w, string name, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                w.WriteAttributeString(name, value);
        }

        private static void WriteElem(XmlWriter w, string name, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                w.WriteStartElement(name);
                w.WriteString(value);
                w.WriteEndElement();
            }
        }
    }
}
