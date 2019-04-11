using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Collections.Generic;

using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;


/* 命名規則

get ... RESTサーバからResourceを取得する場合
create... 最初からResourceを生成する場合
   
 */

namespace eDischargeSummarySample
{
    public class FhirUtil
    {

        public static Observation createLOINCObservation(Patient patient,string code_text,string code,Element value)
        {

            return new Observation()
            {
                Id = FhirUtil.createUUID(),
                Subject = new ResourceReference() { Reference = patient.Id },
                Status = ObservationStatus.Final,
                Code = new CodeableConcept()
                {
                    Text = code_text,
                    Coding = new List<Coding>() {
                            new Coding()
                            {
                                System = "http://loinc.org",
                                Code = code,
                                Display = code_text
                            }

                        }
                },
                Value = value

            };
        }

        public static Attachment CreateAttachmentFromFile(string path)
        {
            //本当はMimeハンドリングとかしっかりやらないといけないんだけど、面倒だし
            //サンプルなので、現時点ではjpeg決め打ちで勘弁してください。 orz

            var attachment = new Attachment()
            {
                ContentType = "image/jpeg",
                Title = "Hydrocephalus",
                Data = File.ReadAllBytes(path)
            };

            return attachment;
        }

        public static string createUUID()
        {
            //[ERROR] Value 'urn:uuid:b35d0997-d1b0-4d71-8f46-de04664c71dd' does not match regex '[A-Za-z0-9\-\.]{1,64}' (at Patient.id[0]
            //とエラーがでるので、urn:uuidをつけるかどうかは、呼び出し元で決めることとし、ここではuuid本体のみ返す。
            //return "urn:uuid:" + System.Guid.NewGuid();

            return System.Guid.NewGuid().ToString();
        }

        public static string generateXML(Resource res)
        {

            var serializer = new FhirXmlSerializer();
            string xmlText = serializer.SerializeToString(res);
            return xmlText;
        }


        public static string tidyXMLString(string xml)
        {
            string result = "";

            MemoryStream mStream = new MemoryStream();
            XmlTextWriter writer = new XmlTextWriter(mStream, Encoding.Unicode);
            XmlDocument document = new XmlDocument();

            try
            {
               
                document.LoadXml(xml);

                writer.Formatting = Formatting.Indented;
                writer.Indentation = 2;
                document.WriteContentTo(writer);
                writer.Flush();
                mStream.Flush();

                mStream.Position = 0;
                StreamReader sReader = new StreamReader(mStream);

                string formattedXml = sReader.ReadToEnd();

                result = formattedXml;
            }
            catch (XmlException)
            {
                // Handle the exception
            }

            writer.Close();
            mStream.Close();


            return result;
        }
    }
}
