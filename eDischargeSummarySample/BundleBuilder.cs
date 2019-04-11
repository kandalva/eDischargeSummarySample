using System;
using Hl7.Fhir.Model;   
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Rest;

using System.Collections.Generic;

using System.IO;
using System.Reflection;

//退院時サマリー V1.41 附属書 B XMLコーディング例をベースに本サンプルを作成した。

//最初にNuget PackageManagerでFHIRのパッケージをインストールする。 HL7.Fhir.STU3

namespace eDischargeSummarySample
{
    public class BundleBuilder
    {
        public static Bundle getBundle(string basePath)
        {
            Bundle bdl = new Bundle();
            bdl.Type = Bundle.BundleType.Document;

            // A document must have an identifier with a system and a value () type = 'document' implies (identifier.system.exists() and identifier.value.exists())

            bdl.Identifier = new Identifier()
            {
                System = "urn:ietf:rfc:3986",
                Value = FhirUtil.createUUID()
            };

            //Composition for Discharge Summary
            Composition c = new Composition();

            //B.2　患者基本情報
            // TODO 退院時サマリー V1.41 B.2 患者基本情報のproviderOrganizationの意図を確認 B.8 受診、入院時情報に入院した
            //医療機関の記述が書かれているならば、ここの医療機関はどういう位置づけのもの？ とりあえず、managingOrganizationにいれておくが。
            Patient patient = null;
            if (true)
            {
                //Patientリソースを自前で生成する場合
                patient = PatientUtil.create();
            }
            else
            {
                //Patientリソースをサーバから取ってくる場合
                #pragma warning disable 0162
                patient = PatientUtil.get();
            }

            Practitioner practitioner = PractitionerUtil.create();
            Organization organization = OrganizationUtil.create();

            patient.ManagingOrganization = new ResourceReference() { Reference = organization.Id };


        //Compositionの作成

        c.Id = FhirUtil.createUUID(); //まだFHIRサーバにあげられていないから，一時的なUUIDを生成してリソースIDとしてセットする
            c.Status = CompositionStatus.Preliminary; //最終版は CompositionStatus.Final
            c.Type = new CodeableConcept()
            {
                Text = "Discharge Summary", //[疑問]Codable Concept内のTextと、Coding内のDisplayとの違いは。Lead Term的な表示？
                Coding = new List<Coding>()
                {
                    new Coding()
                    {
                        Display = "Discharge Summary",
                        System = "http://loinc.org",
                        Code = "18842-5",
                    }
                }
            };

            c.Date = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzzz");
            c.Author = new List<ResourceReference>() { new ResourceReference() { Reference = practitioner.Id } };

            c.Subject = new ResourceReference() { Reference =  patient.Id };

            c.Title = "退院時サマリー"; //タイトルはこれでいいのかな。

            //B.3 承認者

            var attester = new Composition.AttesterComponent();

            var code = new Code<Composition.CompositionAttestationMode>();
            code.Value = Composition.CompositionAttestationMode.Legal;
            attester.ModeElement.Add(code);

            attester.Party = new ResourceReference() { Reference = practitioner.Id };
            attester.Time = "20140620";

            c.Attester.Add(attester);

            //B.4 退院時サマリー記載者

            var author = new Practitioner();
            author.Id = FhirUtil.createUUID();

            var authorName = new HumanName().WithGiven("太郎").AndFamily("本日");
            authorName.Use = HumanName.NameUse.Official;
            authorName.AddExtension("http://hl7.org/fhir/StructureDefinition/iso21090-EN-representation", new FhirString("IDE"));
            author.Name.Add(authorName);

            c.Author.Add(new ResourceReference() { Reference = author.Id });

            //B.5 原本管理者

            c.Custodian = new ResourceReference(){ Reference = organization.Id};

            //B.6 関係者 保険者　何故退院サマリーに保険者の情報を入れているのかな？
            //TODO 未実装


            //sections

            //B.7 主治医
            var section_careteam = new Composition.SectionComponent();
            section_careteam.Title = "careteam";

            var careteam = new CareTeam();
            careteam.Id = FhirUtil.createUUID(); 

            var attendingPhysician = new Practitioner();
            attendingPhysician.Id = FhirUtil.createUUID();

            var attendingPhysicianName = new HumanName().WithGiven("二郎").AndFamily("日本");
            attendingPhysicianName.Use = HumanName.NameUse.Official;
            attendingPhysicianName.AddExtension("http://hl7.org/fhir/StructureDefinition/iso21090-EN-representation", new FhirString("IDE"));
            attendingPhysician.Name.Add(attendingPhysicianName);

            //医師の診療科はPracitionerRole/speciality + PracticeSettingCodeValueSetで表現する.
            //TODO: 膠原病内科は、Specilityのprefered ValueSetの中にはなかった。日本の診療科に関するValueSetを作る必要がある。
            var attendingPractitionerRole = new PractitionerRole();

            attendingPractitionerRole.Id = FhirUtil.createUUID();
            attendingPractitionerRole.Practitioner = new ResourceReference(){ Reference = attendingPhysician.Id };
            attendingPractitionerRole.Code.Add(new CodeableConcept("http://hl7.org/fhir/ValueSet/participant-role", "394733009", "Attending physician"));
            attendingPractitionerRole.Specialty.Add(new CodeableConcept("http://hl7.org/fhir/ValueSet/c80-practice-codes", "405279007", "Medical specialty--OTHER--NOT LISTED"));

            var participant = new CareTeam.ParticipantComponent();
            participant.Member = new ResourceReference() { Reference = attendingPractitionerRole.Id };

            careteam.Participant.Add(participant);


            section_careteam.Entry.Add(new ResourceReference() { Reference = careteam.Id });
            c.Section.Add(section_careteam);


            //B.8 受診、入院情報
            //B.12 本文 (Entry部） 退院時サマリーV1.41の例では入院時診断を本文に書くような形に
            //なっているが、FHIRではadmissionDetailセクション中のEncounterで記載するのが自然だろう。

            var section_admissionDetail = new Composition.SectionComponent();
            section_admissionDetail.Title = "admissionDetail";

            var encounter = new Encounter();
            encounter.Id = FhirUtil.createUUID();

            encounter.Period = new Period() { Start = "20140328", End = "20140404" };

            var hospitalization = new Encounter.HospitalizationComponent();
            hospitalization.DischargeDisposition = new CodeableConcept("\thttp://hl7.org/fhir/discharge-disposition", "01", "Discharged to home care or self care (routine discharge)");

            encounter.Hospitalization = hospitalization;

            var locationComponent = new Encounter.LocationComponent();
            var location = new Location() {
                Id = FhirUtil.createUUID(),
            Name = "○○クリニック",
                    Type = new CodeableConcept("http://terminology.hl7.org/ValueSet/v3-ServiceDeliveryLocationRoleType", "COAG", "Coagulation clinic") };

            locationComponent.Location = new ResourceReference() { Reference = location.Id };

            section_admissionDetail.Entry.Add(new ResourceReference() { Reference = encounter.Id });

          


            var diagnosisAtAdmission = new Condition()
            {
                Code = new CodeableConcept("http://hl7.org/fhir/ValueSet/icd-10", "N801", "右卵巣嚢腫")
            };
            diagnosisAtAdmission.Id = FhirUtil.createUUID();

            var diagnosisComponentAtAdmission = new Encounter.DiagnosisComponent()
            {
                Condition = new ResourceReference() { Reference = diagnosisAtAdmission.Id },
                Role = new CodeableConcept("http://hl7.org/fhir/ValueSet/diagnosis-role", "AD", "Admission diagnosis")
            };

            var encounterAtAdmission = new Encounter();
            encounterAtAdmission.Id = FhirUtil.createUUID();
            encounterAtAdmission.Diagnosis.Add(diagnosisComponentAtAdmission);
            section_admissionDetail.Entry.Add(new ResourceReference() { Reference = encounterAtAdmission.Id });


            c.Section.Add(section_admissionDetail);

            //B.9情報提供者

            var section_informant = new Composition.SectionComponent();
            section_informant.Title = "informant";

            var informant = new RelatedPerson();
            informant.Id = FhirUtil.createUUID();

            var informantName = new HumanName().WithGiven("藤三郎").AndFamily("東京");
            informantName.Use = HumanName.NameUse.Official;
            informantName.AddExtension("http://hl7.org/fhir/StructureDefinition/iso21090-EN-representation", new FhirString("IDE"));
            informant.Name.Add(informantName);
            informant.Patient = new ResourceReference() { Reference = patient.Id };
            informant.Relationship = new CodeableConcept("http://hl7.org/fhir/ValueSet/relatedperson-relationshiptype", "FTH", "father");

            informant.Address.Add( new Address()
            {
                Line = new string[]{ "新宿区神楽坂一丁目50番地" },
                State = "東京都",
                PostalCode ="01803",
                Country = "日本"
            });
           
            informant.Telecom.Add(new ContactPoint()
            {
                System = ContactPoint.ContactPointSystem.Phone,
                Use = ContactPoint.ContactPointUse.Work,
                Value = "tel:(03)3555-1212"
            });


            section_informant.Entry.Add(new ResourceReference() { Reference = informant.Id });
            c.Section.Add(section_informant);


            // B.10 本文
            // B.10.1 本文記述

            var section_clinicalSummary = new Composition.SectionComponent();
            section_clinicalSummary.Title = "来院理由";

            section_clinicalSummary.Code = new CodeableConcept("http://loinc.org", "29299-5", "Reason for visit Narrative");

            var dischargeSummaryNarrative = new Narrative();
            dischargeSummaryNarrative.Status = Narrative.NarrativeStatus.Additional;
            dischargeSummaryNarrative.Div = @"
<div xmlns=""http://www.w3.org/1999/xhtml"">\n\n
<p>平成19年喘息と診断を受けた。平成20年7月喘息の急性増悪にて当院呼吸器内科入院。退院後HL7医院にてFollowされていた</p>
<p>平成21年10月20日頃より右足首にじんじん感が出現。左足首、両手指にも認めるようになった。同時に37℃台の熱発出現しWBC28000、Eosi58%と上昇していた</p>
<p>このとき尿路感染症が疑われセフメタゾン投与されるも改善せず。WBC31500、Eosi64%と上昇、しびれ感の増悪認めた。またHb 7.3 Ht 20.0と貧血を認めた</p>
<p>膠原病、特にChuge-stress-syndoromeが疑われ平成21年11月8日当院膠原病内科入院となった</p>
</div>
            ";

            section_clinicalSummary.Text = dischargeSummaryNarrative;
            c.Section.Add(section_clinicalSummary);

            //B.10.3 観察・検査等
            //section-observations

            var section_observations = new Composition.SectionComponent();
            section_observations.Title = "observations";

            var obs = new List<Observation>()
            {
                FhirUtil.createLOINCObservation(patient,"8302-2","身長", new Quantity()
                    {
                        Value = (decimal)2.004,
                        Unit = "m"
            }),
               FhirUtil.createLOINCObservation(patient,"8302-2","身長", new Quantity()
                    {
                        Value = (decimal)2.004,
                        Unit = "m"

            }),
               //Component Resultsの例　血圧- 拡張期血圧、収縮期血圧のコンポーネントを含む。
               new Observation()
            {
                Id = FhirUtil.createUUID(),
                Subject = new ResourceReference() { Reference = patient.Id },
                Status = ObservationStatus.Final,
                Code = new CodeableConcept()
                {
                    Text = "血圧",
                    Coding = new List<Coding>() {
                            new Coding()
                            {
                                System = "http://loinc.org",
                                Code = "18684-1",
                                Display = "血圧"
                            }

                        }
                },
                Component = new List<Observation.ComponentComponent>()
                {
                    new Observation.ComponentComponent()
                    {
                        Code =  new CodeableConcept()
                {
                    Text = "収縮期血圧血圧",
                    Coding = new List<Coding>() {
                            new Coding()
                            {
                                System = "http://loinc.org",
                                Code = "8480-6",
                                Display = "収縮期血圧"
                            }

                        }
                },
                        Value = new Quantity()
                        {Value = (decimal)120,
                           Unit = "mm[Hg]"

                        }

                    },
                      new Observation.ComponentComponent()
                    {
                                 Code =  new CodeableConcept()
                {
                    Text = "拡張期血圧",
                    Coding = new List<Coding>() {
                            new Coding()
                            {
                                System = "http://loinc.org",
                                Code = "8462-4",
                                Display = "拡張期血圧"
                            }

                        }
                },
                        Value = new Quantity()
                        {Value = (decimal)100,
                           Unit = "mm[Hg]"

                        }

                    },
                }


            }
        };


            foreach (var res in obs)
            {
                section_observations.Entry.Add(new ResourceReference() { Reference = res.Id });
            }

            c.Section.Add(section_observations);


            //B.10.4 キー画像
            //section-medicalImages


            var mediaPath = basePath + "/../../resource/Hydrocephalus_(cropped).jpg";
            var media = new Media();
            media.Id = FhirUtil.createUUID();
            media.Type = Media.DigitalMediaType.Photo;
            media.Content = FhirUtil.CreateAttachmentFromFile(mediaPath);

            /* R3ではImagingStudyに入れられるのはDICOM画像だけみたい。 R4ではreasonReferenceでMediaも参照できる。
             * とりあえずDSTU3ではMediaリソースをmedicalImagesセクションにセットする。
            var imagingStudy = new ImagingStudy();
            imagingStudy.Id = FhirUtil.getUUID(); //まだFHIRサーバにあげられていないから，一時的なUUIDを生成してリソースIDとしてセットする
            imagingStudy.re
            */

            var section_medicalImages = new Composition.SectionComponent();
            section_medicalImages.Title = "medicalImages";
            //TODO: sectionのcodeは？
            section_medicalImages.Entry.Add(new ResourceReference() { Reference = media.Id });
            c.Section.Add(section_medicalImages);


            //Bundleの構築

            //Bundleの構成要素
            //Bunbdleの一番最初はCompostion Resourceであること。
            List<Resource> bdl_entries = new List<Resource> {
                c,patient,organization, practitioner,author,careteam,encounter,encounterAtAdmission,
    diagnosisAtAdmission,location,informant,media,attendingPhysician,attendingPractitionerRole };
            bdl_entries.AddRange(obs);


            foreach (Resource res in bdl_entries)
            {
                var entry = new Bundle.EntryComponent();
                //entry.FullUrl = res.ResourceBase.ToString()+res.ResourceType.ToString() + res.Id;
                entry.FullUrl =  res.Id;
                entry.Resource = res;
                bdl.Entry.Add(entry);
            }

            return bdl;
        }
    }
}
