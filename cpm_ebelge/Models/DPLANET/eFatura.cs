using System;
using System.Xml.Serialization;

namespace cpm_ebelge.Models.DPLANET
{
    public class eFatura
    {
        public override string Gonder()
        {
            base.Gonder();

            var dpEFatura = new DigitalPlanet.InvoiceWebService();
            var dpResult = dpEFatura.EFaturaGonder(eFaturaProtectedValues.strFatura, eFaturaProtectedValues.createdUBL.IssueDate.Value, eFaturaProtectedValues.doc.ENTSABLON);
            if (dpResult.ServiceResult == COMMON.dpInvoice.Result.Error)
            {
                if (appConfig.Debugging)
                    MessageBox.Show(JsonConvert.SerializeObject(dpResult), "ServiceResultDescription", MessageBoxButton.OK, MessageBoxImage.Error);

                throw new Exception(dpResult.ServiceResultDescription);
            }


            Result.DurumAciklama = dpResult.ServiceResultDescription;
            Result.DurumKod = dpResult.ServiceResult == COMMON.dpInvoice.Result.Successful ? "1" : dpResult.ErrorCode + "";
            Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
            Result.EvrakNo = dpResult.Invoices[0].InvoiceId;
            Result.UUID = dpResult.Invoices[0].UUID;
            Result.ZarfUUID = dpResult.InstanceIdentifier;
            Result.YanitDurum = doc.METHOD == "TEMELFATURA" ? 1 : 0;

            Entegrasyon.UpdateEfagdn(Result, EVRAKSN, null);
            if (Connector.m.DokumanIndir)
            {
                try
                {
                    var Gonderilen = dpEFatura.GonderilenEFaturaIndir(Result.UUID);
                    if (appConfig.Debugging)
                    {
                        if (!Directory.Exists("DP_ReqResp"))
                            Directory.CreateDirectory("DP_ReqResp");
                        File.WriteAllBytes($"DP_ReqResp\\File_UpdateEfados_{DateTime.Now:dd_MM_yyyy_HH_mm_ss_ffff}.json", Gonderilen.ReturnValue);
                    }
                    Entegrasyon.UpdateEfados(Gonderilen.ReturnValue);
                }
                catch (Exception ex)
                {
                    if (appConfig.Debugging)
                        appConfig.DebuggingException(ex);

                    throw new Exception(ex.Message, ex);
                }
            }
            return "e-Fatura başarıyla gönderildi. \nEvrak No: " + dpResult.Invoices[0].InvoiceId;
        }

        public override string TopluGonder()
        {
            base.TopluGonder();
            var dpFatura = new DigitalPlanet.InvoiceWebService();
            dpFatura.WebServisAdresDegistir();

            foreach (var Fatura in Faturalar)
            {
                var strFatura = serializer.GetXmlAsString(Fatura.BaseUBL); // XML byte tipinden string tipine dönüştürülür

                var dpResult = dpFatura.EFaturaGonder(strFatura, Fatura.BaseUBL.IssueDate.Value, ENTSABLON);

                if (dpResult.ServiceResult == COMMON.dpInvoice.Result.Error)
                {
                    Connector.m.Hata = true;
                    sb.AppendLine(dpResult.ServiceResultDescription);
                    return sb.ToString();
                }
                else
                {
                    foreach (var doc in dpResult.Invoices)
                    {
                        var fat = dpFatura.GonderilenEFaturaIndir(doc.UUID);
                        XmlSerializer deSerializer = new XmlSerializer(typeof(InvoiceType));
                        InvoiceType inv = (InvoiceType)deSerializer.Deserialize(new MemoryStream(fat.ReturnValue));

                        Result.DurumAciklama = fat.ServiceResultDescription;
                        Result.DurumKod = fat.ServiceResult == COMMON.dpInvoice.Result.Successful ? "1" : dpResult.ErrorCode + "";
                        Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
                        Result.EvrakNo = fat.InvoiceId;
                        Result.UUID = fat.UUID;
                        Result.ZarfUUID = dpResult.InstanceIdentifier;
                        Result.YanitDurum = Faturalar2.FirstOrDefault(elm => elm.UUID.Value == doc.UUID).ProfileID.Value == "TEMELFATURA" ? 1 : 0;

                        int EVRAKSN = Entegrasyon.GetEvraksnFromUUID(new List<string> { Result.UUID })[0];

                        Entegrasyon.UpdateEfagdn(Result, EVRAKSN, fat.ReturnValue);
                        sb.AppendLine("e-Fatura başarıyla gönderildi. \nEvrak No: " + dpResult.Invoices[0].InvoiceId);
                    }
                }
            }
            return sb.ToString();

        }

        public override string Esle()
        {
            var Result = new Results.EFAGDN();
            base.Esle();
            var dpFatura = new DigitalPlanet.InvoiceWebService();
            var dpGelen = dpFatura.GonderilenFatura(Entegrasyon.GetUUIDFromEvraksn(Value)[0]);
            if (dpGelen.ServiceResult == COMMON.dpInvoice.Result.Error)
                throw new Exception(dpGelen.ServiceResultDescription);

            Result.DurumAciklama = dpGelen.StatusDescription;
            Result.DurumKod = dpGelen.StatusCode + "";
            Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, 0);
            Result.EvrakNo = dpGelen.InvoiceId;
            Result.UUID = dpGelen.UUID;
            Result.ZarfUUID = "";
            Result.YanitDurum = 0;
            Entegrasyon.UpdateEfagdn(Result, Value[0], dpGelen.ReturnValue, true);


            Result.DurumAciklama = "";
            switch (dpGelen.StatusCode)
            {
                case 9987:
                    Result.DurumKod = "2";
                    break;
                case 9988:
                    Result.DurumKod = "3";
                    break;
                default:
                    Result.DurumKod = "0";
                    break;
            }
            Entegrasyon.UpdateEfagdnStatus(Result);
            break;

        }

        public override string AlinanFaturalarListesi()
        {
            base.AlinanFaturalarListesi();
            var data = new List<AlinanBelge>();

            var dpFatura = new DigitalPlanet.InvoiceWebService();
            dpFatura.WebServisAdresDegistir();
            dpFatura.Login();

            foreach (var fatura in dpFatura.GelenEfaturalar(StartDate, EndDate).Invoices)
            {
                data.Add(new AlinanBelge
                {
                    EVRAKGUID = Guid.Parse(fatura.UUID),
                    EVRAKNO = fatura.InvoiceId,
                    YUKLEMEZAMAN = fatura.Issuetime,
                    GBETIKET = fatura.SenderPostBoxName,
                    GBUNVAN = fatura.Partyname
                });
            }
            break;
            return data;


        }


        public override string Indir()
        {
            base.Indir();
            var dpFatura = new DigitalPlanet.InvoiceWebService();
            var dpGelen = dpFatura.GelenEfaturalar(day1, day2);
            if (dpGelen.ServiceResult == COMMON.dpInvoice.Result.Error)
                throw new Exception(dpGelen.ServiceResultDescription);

            var dpGelenler = new List<Results.EFAGLN>();
            var dpGelenlerByte = new List<byte[]>();
            foreach (var fatura in dpGelen.Invoices)
            {
                dpGelenler.Add(new Results.EFAGLN
                {
                    DurumAciklama = "",
                    DurumKod = "",
                    DurumZaman = fatura.Issuetime,
                    Etiket = fatura.SenderPostBoxName,
                    EvrakNo = fatura.InvoiceId,
                    UUID = fatura.UUID,
                    VergiHesapNo = fatura.Sendertaxid,
                    ZarfUUID = ""
                });
                var bytes = dpFatura.GelenEfatura(fatura.UUID).ReturnValue;
                dpGelenlerByte.Add(bytes);
            }
            Entegrasyon.InsertEfagln(dpGelenlerByte.ToArray(), dpGelenler.ToArray());
            break;
        }

        public override string Kabul()
        {
            base.Kabul();
            var dpUygulamaYaniti = new DigitalPlanet.InvoiceWebService();
            dpUygulamaYaniti.WebServisAdresDegistir();
            var dpResult = dpUygulamaYaniti.KabulEFatura(GUID);

            if (dpResult.ServiceResult == COMMON.dpInvoice.Result.Error)
                throw new Exception(dpResult.ServiceResultDescription);

            Entegrasyon.UpdateEfaglnStatus(new Results.EFAGLN { ZarfUUID = "" }, GUID, true, Aciklama);
        }

        public override string Red()
        {
            base.Red();
            var dpUygulamaYaniti = new DigitalPlanet.InvoiceWebService();
            dpUygulamaYaniti.WebServisAdresDegistir();
            var dpResult = dpUygulamaYaniti.RedEFatura(GUID, Aciklama);

            if (dpResult.ServiceResult == COMMON.dpInvoice.Result.Error)
                throw new Exception(dpResult.ServiceResultDescription);

            Entegrasyon.UpdateEfaglnStatus(new Results.EFAGLN { ZarfUUID = "" }, GUID, false, Aciklama);
        }

        public override string GonderilenGuncelleByDate()
        {
            base.GonderilenGuncelleByDate();
            var Response = new Results.EFAGDN();

            var dpFatura = new DigitalPlanet.InvoiceWebService();
            var dpGelen = dpFatura.GonderilenFaturalar(day1, day2);
            if (dpGelen.ServiceResult == COMMON.dpInvoice.Result.Error)
                throw new Exception(dpGelen.ServiceResultDescription);
            //var dpYanitlar = dpFatura.GelenUygulamaYanitlari();

            foreach (var fatura in dpGelen.Invoices)
            {
                Response.DurumAciklama = fatura.StatusDescription;
                switch (fatura.StatusCode)
                {
                    case 9987:
                        Response.DurumKod = "2";
                        break;
                    case 9988:
                        Response.DurumKod = "3";
                        break;
                    default:
                        Response.DurumKod = "0";
                        break;
                }
                Response.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, 0);
                Response.EvrakNo = fatura.InvoiceId;
                Response.UUID = fatura.UUID;
                Response.ZarfUUID = "";
                Entegrasyon.UpdateEfagdnStatus(Response);
                Entegrasyon.UpdateEfagdnGonderimDurum(fatura.UUID, Queryable.Contains<int>(new[] { 9987, 9988, 54 }.AsQueryable(), fatura.StatusCode) ? 3 : 2);
            }
        }


        public override string GonderilenGuncelleByList()
        {
            base.GonderilenGuncelleByList();
            Response = new Results.EFAGDN();

            var dpFatura = new DigitalPlanet.InvoiceWebService();
            var dpFaturalar = new List<byte[]>();
            foreach (var UUID in UUIDs)
            {
                var dpGonderilen = dpFatura.GonderilenFatura(UUID);
                if (dpGonderilen.ServiceResult != COMMON.dpInvoice.Result.Error)
                    dpFaturalar.Add(dpGonderilen.ReturnValue);
            }

            foreach (var fatura in dpFaturalar)
                Entegrasyon.UpdateEfados(fatura);
            break;
        }


        public override string GelenEsle()
        {
            base.GelenEsle();
            Result = new List<Results.EFAGLN>();

        }


    }
}
