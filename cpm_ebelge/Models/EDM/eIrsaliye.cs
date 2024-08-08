using System;
using System.Xml.Serialization;

namespace cpm_ebelge.Models.EDM
{
    public class eIrsaliye
    {

        public override string TopluGonder()
        {
            base.TopluGonder();
            var edmIrsaliye = new EDM.DespatchWebService();
            edmIrsaliye.WebServisAdresDegistir();
            edmIrsaliye.Login();
            Connector.m.PkEtiketi = PK;
            var edmResult = edmIrsaliye.TopluEIrsaliyeGonder(Irsaliyeler);

            foreach (var doc in edmResult.DESPATCH)
            {
                var fat = edmIrsaliye.GonderilenEIrsaliyeIndir(doc.UUID);
                XmlSerializer serializer = new XmlSerializer(typeof(DespatchAdviceType));
                DespatchAdviceType inv = (DespatchAdviceType)serializer.Deserialize(new MemoryStream(fat[0].CONTENT.Value));

                Result.DurumAciklama = fat[0].HEADER.STATUS_DESCRIPTION;
                Result.DurumKod = "1";
                Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
                Result.EvrakNo = fat[0].ID;
                Result.UUID = fat[0].UUID;
                Result.ZarfUUID = fat[0].HEADER.ENVELOPE_IDENTIFIER ?? "";

                Entegrasyon.UpdateEirsaliye(Result, Convert.ToInt32(inv.AdditionalDocumentReference.Where(element => element.DocumentTypeCode.Value == "CUST_DES_ID").First().ID.Value), fat[0].CONTENT.Value);
                sb.AppendLine("e-İrsaliye başarıyla gönderildi. \nEvrak No: " + fat[0].ID);
            }
            return sb.ToString();

        }

        public override string Gonder()
        {
            base.Gonder();
            var edmIrsaliye = new EDM.DespatchWebService();
            edmIrsaliye.Login();

            var edmResult = edmIrsaliye.EIrsaliyeGonder(eIrsaliyeProtectedValues.strFatura, eIrsaliyeProtectedValues.doc.DeliveryCustomerParty.Party.PartyIdentification[0].ID.Value, Connector.m.VknTckn); ;

            var edmFaturaUBL = edmIrsaliye.GonderilenEIrsaliyeIndir(edmResult.DESPATCH[0].UUID);

            Result.DurumAciklama = edmFaturaUBL[0].HEADER.STATUS_DESCRIPTION;
            Result.DurumKod = "1";
            Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
            Result.EvrakNo = edmFaturaUBL[0].ID;
            Result.UUID = edmFaturaUBL[0].UUID;
            Result.ZarfUUID = edmFaturaUBL[0].HEADER.ENVELOPE_IDENTIFIER ?? "";

            Entegrasyon.UpdateEirsaliye(Result, EVRAKSN, edmFaturaUBL[0].CONTENT.Value);
            return "e-İrsaliye başarıyla gönderildi. \nEvrak No: " + edmFaturaUBL[0].ID;

        }

        public override string Esle()
        {
            base.Esle();
            var Result = new Results.EFAGDN();
            var edmIrsaliye = new EDM.DespatchWebService();
            edmIrsaliye.WebServisAdresDegistir();
            edmIrsaliye.Login();

            var edmGonderilenler = edmIrsaliye.EIrsaliyeIndir(GUID);

            foreach (var ynt in edmGonderilenler)
            {
                Result.DurumAciklama = ynt.HEADER.GIB_STATUS_CODESpecified ? ynt.HEADER.GIB_STATUS_DESCRIPTION : "0";
                Result.DurumKod = ynt.HEADER.GIB_STATUS_CODESpecified ? ynt.HEADER.GIB_STATUS_CODE + "" : "0";
                Result.DurumZaman = ynt.HEADER.ISSUE_DATE;
                Result.EvrakNo = ynt.ID;
                Result.UUID = ynt.UUID;
                Result.ZarfUUID = "";

                Entegrasyon.UpdateEfagdnStatus(Result);
            }
            break;

        }

        public override string Esle()
        {
            base.Esle();
            var Result = new Results.EFAGDN();
            var start = new DateTime(GonderimTarih.Year, GonderimTarih.Month, GonderimTarih.Day, 0, 0, 0);
            var end = new DateTime(GonderimTarih.Year, GonderimTarih.Month, GonderimTarih.Day, 23, 59, 59);
            var edmIrsaliye = new EDM.DespatchWebService();
            edmIrsaliye.WebServisAdresDegistir();
            edmIrsaliye.Login();

            var edmGonderilenler = edmIrsaliye.GonderilenEIrsaliyeler(start, end);

            foreach (var ynt in edmGonderilenler)
            {
                XmlSerializer serializer = new XmlSerializer(typeof(DespatchAdviceType));
                var irs = (DespatchAdviceType)serializer.Deserialize(new MemoryStream(ynt.CONTENT.Value));

                var custInv = irs.AdditionalDocumentReference.Where(elm => elm.DocumentTypeCode.Value == "CUST_DES_ID");
                if (custInv.Any())
                {
                    Result.DurumAciklama = ynt.HEADER.GIB_STATUS_CODESpecified ? ynt.HEADER.GIB_STATUS_DESCRIPTION : "0";
                    Result.DurumKod = ynt.HEADER.GIB_STATUS_CODESpecified ? ynt.HEADER.GIB_STATUS_CODE + "" : "0";
                    Result.DurumZaman = ynt.HEADER.ISSUE_DATE;
                    Result.EvrakNo = ynt.ID;
                    Result.UUID = ynt.UUID;
                    Result.ZarfUUID = "";

                    ///Entegrasyon.UpdateEfagdnStatus(Result);
                    Entegrasyon.UpdateEIrsaliye(Result);
                    break;
                }
            }
            break;

        }

        public override string Indir()
        {
            base.Indir();
            var Result = new Results.EFAGLN();

            var edmIrsaliye = new EDM.DespatchWebService();
            edmIrsaliye.Login();
            Connector.m.IssueDate = new DateTime(day1.Year, day1.Month, day1.Day, 0, 0, 0);
            Connector.m.EndDate = new DateTime(day2.Year, day2.Month, day2.Day, 23, 59, 59);

            var edmGelen = edmIrsaliye.GenelEIrsaliyeler();
            if (edmGelen.Count() > 0)
            {
                foreach (var ubl in edmGelen)
                {
                    Connector.m.GbEtiketi = ubl.HEADER.FROM;
                    eIrsaliyeProtectedValues.doc = edmIrsaliye.EIrsaliyeIndir(ubl.UUID);

                    Result.DurumAciklama = eIrsaliyeProtectedValues.doc[0].HEADER.GIB_STATUS_CODESpecified ? eIrsaliyeProtectedValues.doc[0].HEADER.GIB_STATUS_DESCRIPTION : "0";
                    Result.DurumKod = eIrsaliyeProtectedValues.doc[0].HEADER.GIB_STATUS_CODESpecified ? eIrsaliyeProtectedValues.doc[0].HEADER.GIB_STATUS_CODE + "" : "0";
                    Result.DurumZaman = eIrsaliyeProtectedValues.doc[0].HEADER.ISSUE_DATE;
                    Result.Etiket = eIrsaliyeProtectedValues.doc[0].HEADER.FROM;
                    Result.EvrakNo = eIrsaliyeProtectedValues.doc[0].ID;
                    Result.UUID = eIrsaliyeProtectedValues.doc[0].UUID;
                    Result.VergiHesapNo = eIrsaliyeProtectedValues.doc[0].HEADER.SENDER;
                    Result.ZarfUUID = eIrsaliyeProtectedValues.doc[0].HEADER.ENVELOPE_IDENTIFIER ?? "";

                    Entegrasyon.InsertIrsaliye(Result, eIrsaliyeProtectedValues.doc[0].CONTENT.Value);
                }
            }
            break;

        }

        public override string Kabul()
        {
            base.Kabul();
            throw new Exception("Entegratör Bu Eylemi Desteklememektedir!");

        }

        public override string Red()
        {
            base.Red();
            throw new Exception("Entegratör Bu Eylemi Desteklememektedir!");

        }

        public override string GonderilenGuncelleByDate()
        {
            base.GonderilenGuncelleByDate();
            var Result = new Results.EFAGDN();

            var edmIrsaliye = new EDM.DespatchWebService();
            edmIrsaliye.WebServisAdresDegistir();
            edmIrsaliye.Login();

            var edmGonderilenler = edmIrsaliye.GonderilenEIrsaliyeler(start, end);

            foreach (var ynt in edmGonderilenler)
            {
                Result.DurumAciklama = ynt.HEADER.GIB_STATUS_CODESpecified ? ynt.HEADER.GIB_STATUS_DESCRIPTION : "0";
                Result.DurumKod = ynt.HEADER.GIB_STATUS_CODESpecified ? ynt.HEADER.GIB_STATUS_CODE + "" : "0";
                Result.DurumZaman = ynt.HEADER.ISSUE_DATE;
                Result.EvrakNo = ynt.ID;
                Result.UUID = ynt.UUID;
                Result.ZarfUUID = "";

                Entegrasyon.UpdateEfagdnStatus(Result);
            }
            break;

        }

        public override string GonderilenGuncelle()
        {
            base.GonderilenGuncelle();
            var Result = new Results.EFAGDN();

            var edmIrsaliye = new EDM.DespatchWebService();
            edmIrsaliye.WebServisAdresDegistir();
            edmIrsaliye.Login();


            List<byte[]> edmDosyalar = new List<byte[]>();
            foreach (var UUID in UUIDs)
                edmDosyalar.Add(edmIrsaliye.EIrsaliyeIndir(UUID)[0].CONTENT.Value);

            foreach (var dosya in edmDosyalar)
                Entegrasyon.UpdateEfadosIrsaliye(dosya);
            break;

        }


    }
}
