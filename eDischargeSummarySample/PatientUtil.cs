using System;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;


namespace eDischargeSummarySample
{
    public class PatientUtil
    {
    static public Patient get()
        {
            //PublicなFHIR ServerからPatientリソースを取得
            var Client = new FhirClient("https://vonk.fire.ly");
            Client.PreferredFormat = ResourceFormat.Xml;
            Client.PreferredReturn = Prefer.ReturnRepresentation;

            var pat = Client.Read<Patient>("Patient/d6d9cfc8-5bb6-46c7-938f-31c587f2a6d1");

            return pat;

        }

        static public Patient create()
        {
            // example Patient setup, fictional data only
            var pat = new Patient();

            //まだFHIR Serverに登録されていないので，一時的なリソースIDを生成する
            pat.Id = FhirUtil.createUUID();


            var id = new Identifier();
            id.System = "2.16.840.1.113883.2.2.3.10.1.2\""; // ex. "http://hl7.org/fhir/sid/us-ssn";
            id.Value = "111111";
            pat.Identifier.Add(id);

            var address = new Address()
            {
                Line = new string[] { "新橋2丁目5番5号" },
                City = "港区",
                State = "東京都",
                PostalCode = "105-0004",
                Country = "日本",
                Use = Address.AddressUse.Home
            };

            pat.Address.Add(address);

            var contact = new Patient.ContactComponent();
            contact.Telecom.Add(new ContactPoint(ContactPoint.ContactPointSystem.Phone, null, "tel:03-3506-8010"));
            pat.Contact.Add(contact);

            var sylName = new HumanName().WithGiven("ハナコ").AndFamily("トウキョウ");
            sylName.Use = HumanName.NameUse.Official;
            sylName.AddExtension("http://hl7.org/fhir/StructureDefinition/iso21090-EN-representation", new FhirString("SYL"));


            var ideName = new HumanName().WithGiven("花子").AndFamily("東京");
            ideName.Use = HumanName.NameUse.Official;
            ideName.AddExtension("http://hl7.org/fhir/StructureDefinition/iso21090-EN-representation", new FhirString("IDE"));

            var abcName = new HumanName().WithGiven("Hanako").AndFamily("Tokyo");
            abcName.Use = HumanName.NameUse.Official;
            abcName.AddExtension("http://hl7.org/fhir/StructureDefinition/iso21090-EN-representation", new FhirString("IDE"));


            pat.Name.Add(sylName);
            pat.Name.Add(ideName);
            pat.Name.Add(abcName);

            pat.Gender = AdministrativeGender.Female;

            pat.MaritalStatus = new CodeableConcept("http://hl7.org/fhir/ValueSet/marital-status", "M");

            pat.BirthDate = "1937-07-23";

            var birthplace = new Extension();
            birthplace.Url = "http://hl7.org/fhir/StructureDefinition/birthPlace";
            birthplace.Value = new Address() { City = "葛飾区", Line = new string[] { "亀有公園前派出所" } };
            pat.Extension.Add(birthplace);

            var birthtime = new Extension("http://hl7.org/fhir/StructureDefinition/patient-birthTime",
                                           new FhirDateTime(1983, 4, 23, 7, 44,0,TimeSpan.FromHours(9)));
            pat.BirthDateElement.Extension.Add(birthtime);
           
            pat.Deceased = new FhirBoolean(false);

           


            //System.Console.WriteLine(FhirUtil.tidyXMLString(FhirUtil.generateXML(pat)));

            return pat;
        }
    }
}
