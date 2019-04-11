using System;
using Hl7.Fhir.Model;

namespace eDischargeSummarySample
{
    public class PractitionerUtil
    {
     public static Practitioner create()
        {
            Practitioner p = new Practitioner();

            //まだFHIR Serverに登録されていないので，一時的なリソースIDを生成する
            p.Id = FhirUtil.createUUID();

            var name = new HumanName().WithGiven("太郎").AndFamily("医師");
            name.Use = HumanName.NameUse.Official;
            name.AddExtension("http://hl7.org/fhir/StructureDefinition/iso21090-EN-representation", new FhirString("IDE"));

            p.Name.Add(name);

            return p;
        }
    }
}
