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


    }
}
