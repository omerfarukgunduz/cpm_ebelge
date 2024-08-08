using System;
using System.Runtime.ConstrainedExecution;

namespace cpm_ebelge.Models.EDM
{
    public class eArsiv
    {
        public override string Gonder()
        {
            base.Gonder();
            var edmEArsiv = new EDM.ArchiveWebService();
            var edmResult = edmEArsiv.EArsivGonder(eArsivProtectedValues.strFatura, eArsivProtectedValues.createdUBL.AccountingCustomerParty.Party.PartyIdentification[0].ID.Value, Connector.m.VknTckn, eArsivProtectedValues.createdUBL?.AccountingCustomerParty?.Party?.Contact?.ElectronicMail?.Value ?? "");

            Connector.m.FaturaUUID = edmResult.INVOICE[0].UUID;
            Connector.m.FaturaID = edmResult.INVOICE[0].ID;
            var edmFaturaUBL = edmEArsiv.EArsivIndir(edmResult.INVOICE[0].UUID);
            ByteData = edmFaturaUBL[0].CONTENT.Value;

            Result.DurumAciklama = edmFaturaUBL[0].HEADER.STATUS_DESCRIPTION;
            Result.DurumKod = "";
            Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
            Result.EvrakNo = edmResult.INVOICE[0].ID;
            Result.UUID = edmResult.INVOICE[0].UUID;
            Result.ZarfUUID = "";
            Result.YanitDurum = 0;

            Entegrasyon.UpdateEfagdn(Result, EVRAKSN, ByteData);

            if (eArsivProtectedValues.doc.PRINT)
            {
                response.KagitNusha = true;
                response.Mesaj = "e-Arşiv Fatura başarıyla gönderildi. \nEvrak No: " + Result.EvrakNo + "\nYazdırmak İster Misiniz?";
                response.Dosya = ByteData;
            }
            else
            {
                response.KagitNusha = false;
                response.Mesaj = "e-Arşiv Fatura başarıyla gönderildi. \nEvrak No: " + Result.EvrakNo;
                response.Dosya = null;
            }

            return response;


        }


        public override string TopluGonder()
        {
            base.TopluGonder();
            var edmEArsiv = new EDM.ArchiveWebService();
            edmEArsiv.WebServisAdresDegistir();
            var edmResult = edmEArsiv.TopluEArsivGonder(Faturalar);

            foreach (var a in edmResult.INVOICE)
            {
                foreach (var eArsivProtectedValues.doc in Faturalar)
                        {
                    if (eArsivProtectedValues.doc.UUID.Value == a.UUID)
                    {
                        sb.AppendLine("e-Arşiv Fatura başarıyla gönderildi. \nEvrak No: " + a.ID);

                        Result.DurumAciklama = a.HEADER.STATUS_DESCRIPTION;
                        Result.DurumKod = "";
                        Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
                        Result.EvrakNo = a.ID;
                        Result.UUID = a.UUID;
                        Result.ZarfUUID = a.HEADER.ENVELOPE_IDENTIFIER ?? "";
                        Result.YanitDurum = 0;

                        Entegrasyon.UpdateEfagdn(Result, Convert.ToInt32(eArsivProtectedValues.doc.AdditionalDocumentReference.Where(element => element.DocumentTypeCode.Value == "CUST_INV_ID").First().ID.Value), ser.GetXmlAsByteArray(eArsivProtectedValues.doc));
                        break;
                    }
                }
            }
            return sb.ToString();
        }

        public override string Iptal()
        {
            base.Iptal();
            var edmEArsiv = new EDM.ArchiveWebService();
            var edmpResult = edmEArsiv.IptalFatura(GUID, TUTAR);

            Entegrasyon.SilEarsiv(EVRAKSN, TUTAR, "", DateTime.Now);

            return "eArşiv Fatura Başarıyla İptal Edilmiştir ve İlgili eArşiv Fatura Silinmiştir!";
        }

        public override string Itiraz()
        {
            base.Itiraz();
            var edmArsiv = new EDM.ArchiveWebService();
            edmArsiv.WebServisAdresDegistir();
            edmArsiv.EArsivItiraz();
            return "Test!";
        }
        public override string GonderilenGuncelle()
        {
            base.GonderilenGuncelle();
            var edmArsiv = new EDM.ArchiveWebService();
            var edmGelen = edmArsiv.EArsivIndir(Entegrasyon.GetUUIDFromEvraksn(new List<int> { EVRAKSN })[0]);

            if (edmGelen.Length > 0)
            {
                Result.DurumAciklama = edmGelen[0].HEADER.STATUS_DESCRIPTION;
                Result.DurumKod = "";
                Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
                Result.EvrakNo = edmGelen[0].ID;
                Result.UUID = edmGelen[0].UUID;
                Result.ZarfUUID = edmGelen[0].HEADER.ENVELOPE_IDENTIFIER ?? "";
                Result.YanitDurum = 0;

                Entegrasyon.UpdateEfagdn(Result, EVRAKSN, edmGelen[0].CONTENT.Value, onlyUpdate: true);

                Result.DurumAciklama = edmGelen[0].HEADER.RESPONSE_CODE == "REJECT" ? "" : "";
                Result.DurumKod = edmGelen[0].HEADER.RESPONSE_CODE == "REJECT" ? "3" : (edmGelen[0].HEADER.RESPONSE_CODE == "ACCEPT" ? "2" : "");
                Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
                Result.UUID = edmGelen[0].UUID;

                Entegrasyon.UpdateEfagdnStatus(Result);

                //File.WriteAllText("edm.json", JsonConvert.SerializeObject(edmGelen));

                if (edmGelen[0].HEADER.EARCHIVE_REPORT_UUID != null)
                    Entegrasyon.UpdateEfagdnGonderimDurum(edmGelen[0].UUID, 4);
                else if (edmGelen[0].HEADER.STATUS_DESCRIPTION == "SUCCEED")
                    Entegrasyon.UpdateEfagdnGonderimDurum(edmGelen[0].UUID, 3);
            }
            else
                throw new Exception(EVRAKSN + " Seri Numaralı Evrak Gönderilenler Listesinde Bulunmamaktadır!");
            break;

        }


    }
}
