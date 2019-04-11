using System;
using Hl7.Fhir.Model;

namespace eDischargeSummarySample
{
    public class OrganizationUtil
    {
        public static Organization create()
        {
            Organization o = new Organization();

            //まだFHIR Serverに登録されていないので，一時的なリソースIDを生成する
            o.Id = FhirUtil.createUUID();

            o.Name = "日本HL7新橋病院";
            o.Telecom.Add(new ContactPoint()
            {
                Use = ContactPoint.ContactPointUse.Work,
                Value = "03-3267-1921"
            });
            o.Address.Add(new Address()
            {
                Line = new string[] { "神楽坂一丁目1番地" },
                City = "新宿区",
                State = "東京都",
                PostalCode = "162-0825",
                Country = "Japan"
            });
            return o;
        }
    }
}
