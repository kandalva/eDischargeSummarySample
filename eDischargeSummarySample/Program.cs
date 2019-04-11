using System;
using System.IO;
using System.Reflection;
using Hl7.Fhir.Model;  
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Validation;


using System.Collections.Generic;

//退院時サマリー V1.41 附属書 B XMLコーディング例をベースに本サンプルを作成した。
//最初にNuget PackageManagerでFHIRのパッケージをインストールする。 HL7.Fhir.STU3

namespace eDischargeSummarySample
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Bundle bdl = BundleBuilder.getBundle(basePath);

            var xmlText = FhirUtil.tidyXMLString(FhirUtil.generateXML(bdl));

            //System.Console.WriteLine(xmlText);
            File.WriteAllText(basePath+@"/../../sample/eDischargeSample.xml", xmlText);

        }
    }
}
