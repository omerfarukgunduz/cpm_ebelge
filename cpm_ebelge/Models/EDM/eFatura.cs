using System;
using System.Xml.Serialization;

namespace cpm_ebelge.Models.EDM
{
    public class eFatura
    {
        public override string Gonder()
        {
            base.Gonder();
            var edmEFatura = new EDM.InvoiceWebService();
            createdUBL.ID = new UblInvoiceObject.IDType { Value = "ABC2009123456789" };
            strFatura = serializer.GetXmlAsString(createdUBL);
            var edmResult = edmEFatura.EFaturaGonder(strFatura, Connector.m.PkEtiketi, Connector.m.GbEtiketi, Connector.m.VknTckn, createdUBL.AccountingSupplierParty.Party.PartyIdentification[0].ID.Value);

            var edmInvoice = edmEFatura.GonderilenEFaturaIndir(edmResult.INVOICE[0].UUID);

            Result.DurumAciklama = edmInvoice[0].HEADER.STATUS_DESCRIPTION;
            Result.DurumKod = "1";
            Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
            Result.EvrakNo = edmResult.INVOICE[0].ID;
            Result.UUID = edmResult.INVOICE[0].UUID;
            Result.ZarfUUID = edmInvoice[0].HEADER.ENVELOPE_IDENTIFIER ?? "";
            Result.YanitDurum = doc.METHOD == "TEMELFATURA" ? 1 : 0;

            Entegrasyon.UpdateEfagdn(Result, EVRAKSN, edmInvoice[0].CONTENT.Value);

            return "e-Fatura başarıyla gönderildi. \nEvrak No: " + edmResult.INVOICE[0].ID;

        }

        public override string TopluGonder()
        {
            base.TopluGonder();
            var edmFatura = new EDM.InvoiceWebService();
            edmFatura.WebServisAdresDegistir();
            var edmResult = edmFatura.TopluEFaturaGonder(Faturalar2);

            foreach (var doc in edmResult.INVOICE)
            {
                var fat = edmFatura.GonderilenEFaturaIndir(doc.UUID);
                XmlSerializer deSerializer = new XmlSerializer(typeof(InvoiceType));
                InvoiceType inv = (InvoiceType)deSerializer.Deserialize(new MemoryStream(fat[0].CONTENT.Value));

                Result.DurumAciklama = fat[0].HEADER.STATUS_DESCRIPTION;
                Result.DurumKod = "1";
                Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
                Result.EvrakNo = edmResult.INVOICE[0].ID;
                Result.UUID = edmResult.INVOICE[0].UUID;
                Result.ZarfUUID = fat[0].HEADER.ENVELOPE_IDENTIFIER ?? "";
                Result.YanitDurum = Faturalar2.FirstOrDefault(elm => elm.UUID.Value == doc.UUID).ProfileID.Value == "TEMELFATURA" ? 1 : 0;

                Entegrasyon.UpdateEfagdn(Result, Convert.ToInt32(inv.AdditionalDocumentReference.Where(element => element.DocumentTypeCode.Value == "CUST_INV_ID").First().ID.Value), fat[0].CONTENT.Value);
                sb.AppendLine("e-Fatura başarıyla gönderildi. \nEvrak No: " + Result.EvrakNo);
            }
            return sb.ToString();
        }


        public override string Esle()
        {
            var Result = new Results.EFAGDN();
            var edmFatura = new EDM.InvoiceWebService();
            var edmGelen = edmFatura.GonderilenEFaturaIndir(Entegrasyon.GetUUIDFromEvraksn(Value)[0]);

            Result.DurumAciklama = edmGelen[0].HEADER.GIB_STATUS_CODESpecified ? edmGelen[0].HEADER.GIB_STATUS_DESCRIPTION : "0";
            Result.DurumKod = edmGelen[0].HEADER.GIB_STATUS_CODESpecified ? edmGelen[0].HEADER.GIB_STATUS_CODE + "" : "0";
            Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
            Result.EvrakNo = edmGelen[0].ID;
            Result.UUID = edmGelen[0].UUID;
            Result.ZarfUUID = edmGelen[0].HEADER.ENVELOPE_IDENTIFIER ?? "";
            Result.YanitDurum = 0;

            Entegrasyon.UpdateEfagdn(Result, Value[0], edmGelen[0].CONTENT.Value, true);

            Result.DurumAciklama = edmGelen[0].HEADER.RESPONSE_CODE == "REJECT" ? "" : "";
            Result.DurumKod = edmGelen[0].HEADER.RESPONSE_CODE == "REJECT" ? "3" : (edmGelen[0].HEADER.RESPONSE_CODE == "ACCEPT" ? "2" : "");
            Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
            Result.UUID = edmGelen[0].UUID;

            Entegrasyon.UpdateEfagdnStatus(Result);

            if (edmGelen[0].HEADER.STATUS_DESCRIPTION == "SUCCEED")
                Entegrasyon.UpdateEfagdnGonderimDurum(edmGelen[0].UUID, 3);
            break;
        }


        public override string AlinanFaturalarListesi()
        {
            base.AlinanFaturalarListesi();
            var data = new List<AlinanBelge>();


            return data;

        }


        public override string Indir()
        {
            base.Indir();
            var edmFatura = new EDM.InvoiceWebService();
            var edmGelen = edmFatura.GelenEfaturalar(day1, day2);

            var edmGelenler = new List<Results.EFAGLN>();
            var edmGelenlerByte = new List<byte[]>();
            foreach (var fatura in edmGelen)
            {
                edmGelenler.Add(new Results.EFAGLN
                {
                    DurumAciklama = fatura.HEADER.GIB_STATUS_CODESpecified ? fatura.HEADER.GIB_STATUS_DESCRIPTION : "0",
                    DurumKod = fatura.HEADER.GIB_STATUS_CODESpecified ? fatura.HEADER.GIB_STATUS_CODE + "" : "0",
                    DurumZaman = fatura.HEADER.ISSUE_DATE,
                    Etiket = fatura.HEADER.FROM,
                    EvrakNo = fatura.ID,
                    UUID = fatura.UUID,
                    VergiHesapNo = fatura.HEADER.SENDER,
                    ZarfUUID = fatura.HEADER.ENVELOPE_IDENTIFIER ?? ""
                });
                edmGelenlerByte.Add(fatura.CONTENT.Value);
            }
            Entegrasyon.InsertEfagln(edmGelenlerByte.ToArray(), edmGelenler.ToArray());
            break;
        }


        public override string Kabul()
        {
            base.Kabul();
            var edmUygulamaYaniti = new EDM.InvoiceWebService();
            edmUygulamaYaniti.WebServisAdresDegistir();
            var edmResult = edmUygulamaYaniti.KabulEFatura(GUID, Aciklama);

            Entegrasyon.UpdateEfaglnStatus(new Results.EFAGLN { ZarfUUID = "" }, GUID, true, Aciklama);
        }


        public override string Red()
        {
            base.Red();
            var edmUygulamaYaniti = new EDM.InvoiceWebService();
            edmUygulamaYaniti.WebServisAdresDegistir();
            var edmResult = edmUygulamaYaniti.RedEFatura(GUID, Aciklama);

            Entegrasyon.UpdateEfaglnStatus(new Results.EFAGLN { ZarfUUID = "" }, GUID, false, Aciklama);
        }

        public override string GonderilenGuncelleByDate()
        {
            base.GonderilenGuncelleByDate();
            var Response = new Results.EFAGDN();

            var edmFatura = new EDM.InvoiceWebService();
            var edmGelen = edmFatura.GonderilenFaturalar(day1, day2);

            //var edmYanitlar = edmFatura.GelenUygulamaYanitlari(day1, day2);

            foreach (var fatura in edmGelen)
            {
                if (fatura.HEADER.STATUS_DESCRIPTION == "SUCCEED")
                    Entegrasyon.UpdateEfagdnGonderimDurum(fatura.UUID, 3);

                Response.DurumAciklama = fatura.HEADER.RESPONSE_CODE == "REJECT" ? "" : "";
                Response.DurumKod = fatura.HEADER.RESPONSE_CODE == "REJECT" ? "3" : (fatura.HEADER.RESPONSE_CODE == "ACCEPT" ? "2" : "");
                Response.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
                Response.UUID = fatura.UUID;

                Entegrasyon.UpdateEfagdnStatus(Response);
            }
        }

        public override string GonderilenGuncelleByList()
        {
            base.GonderilenGuncelleByList();
            Response = new Results.EFAGDN();

        }


        public override string GelenEsle()
        {
            base.GelenEsle();
            Result = new List<Results.EFAGLN>();


        }


    }
}
