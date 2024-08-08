using System.Xml.Serialization;
using System;

namespace cpm_ebelge.Models.FIT
{
    public class eIrsaliye
    {
        public override string TopluGonder()
        {
            base.TopluGonder();
            var fitIrsaliye = new FIT.DespatchWebService();
            fitIrsaliye.WebServisAdresDegistir();
            Connector.m.PkEtiketi = PK;
            var fitResult = fitIrsaliye.TopluIrsaliyeGonder(Irsaliyeler);
            var fitEnvResult = fitIrsaliye.ZarfDurumSorgula(new[] { fitResult.Response[0].EnvUUID });

            foreach (var doc in fitResult.Response)
            {
                //var fat = fitIrsaliye.GonderilenIrsaliyeUBLIndir(new[] { doc.UUID });
                Result.DurumAciklama = fitEnvResult[0].Description;
                Result.DurumKod = fitEnvResult[0].ResponseCode;
                Result.DurumZaman = fitEnvResult[0].IssueDate;
                Result.EvrakNo = doc.ID;
                Result.UUID = doc.UUID;
                Result.ZarfUUID = doc.EnvUUID;

                //Entegrasyon.UpdateEirsaliye(Result, Convert.ToInt32(doc.CustDesID), fat.Response[0].DocData);
                Entegrasyon.UpdateEirsaliye(Result, Convert.ToInt32(doc.CustDesID), null);
                if (Connector.m.DokumanIndir)
                {
                    var Gonderilen = fitIrsaliye.GonderilenIrsaliyeUBLIndir(new[] { Result.UUID });
                    Entegrasyon.UpdateEfadosIrsaliye(Gonderilen.Response[0].DocData);
                }
                sb.AppendLine("e-İrsaliye başarıyla gönderildi. \nEvrak No: " + Result.EvrakNo);
            }
            return sb.ToString();

        }

        public override string Gonder()
        {
            base.Gonder();
            var fitIrsaliye = new FIT.DespatchWebService();
            fitIrsaliye.WebServisAdresDegistir();

            var fitResult = fitIrsaliye.IrsaliyeGonder(eIrsaliyeProtectedValues.strFatura, eIrsaliyeProtectedValues.doc.UUID.Value);

            //var fitFaturaUBL = fitIrsaliye.GonderilenIrsaliyeUBLIndir(new[] { fitResult.Response[0].UUID });
            //var faturaBytes = ZipUtility.UncompressFile(fitFaturaUBL.Response[0].DocData);

            Result.DurumAciklama = "";
            Result.DurumKod = "1";
            Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
            Result.EvrakNo = fitResult.Response[0].ID;
            Result.UUID = fitResult.Response[0].UUID;
            Result.ZarfUUID = fitResult.Response[0].EnvUUID;

            //Entegrasyon.UpdateEirsaliye(Result, EVRAKSN, faturaBytes);
            Entegrasyon.UpdateEirsaliye(Result, EVRAKSN, null);
            if (Connector.m.DokumanIndir)
            {
                var Gonderilen = fitIrsaliye.GonderilenIrsaliyeUBLIndir(new[] { Result.UUID });
                Entegrasyon.UpdateEfadosIrsaliye(ZipUtility.UncompressFile(Gonderilen.Response[0].DocData));
            }
            return "e-İrsaliye başarıyla gönderildi. \nEvrak No: " + fitResult.Response[0].ID;

        }

        public override string Esle()
        {

            base.Esle();
            var Result = new Results.EFAGDN();
            var fitIrsaliye = new FIT.DespatchWebService();
            fitIrsaliye.WebServisAdresDegistir();

            var fitIrsaliyeler = fitIrsaliye.GonderilenIrsaliyIndir(new[] { GUID });

            foreach (var irs in fitIrsaliyeler.Response)
            {
                var fitZarf = fitIrsaliye.GonderilenZarflarIndir(new[] { irs.EnvUUID + "" });
                Result.DurumAciklama = fitZarf.Response[0].Description;
                Result.DurumKod = fitZarf.Response[0].ResponseCode;
                Result.DurumZaman = fitZarf.Response[0].IssueDate;
                Result.EvrakNo = irs.ID;
                Result.UUID = irs.UUID;
                Result.ZarfUUID = irs.EnvUUID + "";

                ///Entegrasyon.UpdateEfagdnStatus(Result);
                Entegrasyon.UpdateEIrsaliye(Result);
            }

            var fitYanitlar = fitIrsaliye.IrsaliyeYanitiIndir(GUID);
            if (fitYanitlar.Response.Length > 0)
            {
                if (fitYanitlar.Response[0].Receipts != null)
                {
                    foreach (var fitYanit in fitYanitlar.Response[0].Receipts)
                    {
                        var recData = fitYanit.DocData;

                        XmlSerializer ser = new XmlSerializer(typeof(ReceiptAdviceType));
                        ReceiptAdviceType receipt = (ReceiptAdviceType)ser.Deserialize(new MemoryStream(ZipUtility.UncompressFile(recData)));

                        var rejected = receipt.ReceiptLine.Any(elm => elm.RejectedQuantity?.Value != null);

                        if (rejected)
                        {
                            var Result = new Results.EFAGDN
                            {
                                DurumAciklama = receipt.ReceiptLine.FirstOrDefault(elm => elm.RejectedQuantity?.Value != null).RejectReason?[0]?.Value ?? "",
                                DurumKod = "3",
                                DurumZaman = receipt.IssueDate.Value,
                                UUID = fitYanitlar.Response[0].DespatchUUID,
                            };

                            Entegrasyon.UpdateEfagdnStatus(Result);
                        }
                    }
                }
            }
            break;

        }

        public override string Esle()
        {

            base.Esle();
            var Result = new Results.EFAGDN();
            var start = new DateTime(GonderimTarih.Year, GonderimTarih.Month, GonderimTarih.Day, 0, 0, 0);
            var end = new DateTime(GonderimTarih.Year, GonderimTarih.Month, GonderimTarih.Day, 23, 59, 59);
            var fitIrsaliye = new FIT.DespatchWebService();
            fitIrsaliye.WebServisAdresDegistir();

            var fitIrsaliyeler = fitIrsaliye.GonderilenIrsaliyIndir(new[] { EVRAKGUID });

            foreach (var irs in fitIrsaliyeler.Response)
            {
                var fitZarf = fitIrsaliye.GonderilenZarflarIndir(new[] { irs.EnvUUID + "" });
                Result.DurumAciklama = fitZarf.Response[0].Description;
                Result.DurumKod = fitZarf.Response[0].ResponseCode;
                Result.DurumZaman = fitZarf.Response[0].IssueDate;
                Result.EvrakNo = irs.ID;
                Result.UUID = irs.UUID;
                Result.ZarfUUID = irs.EnvUUID + "";

                ///Entegrasyon.UpdateEfagdnStatus(Result);
                Entegrasyon.UpdateEIrsaliye(Result);
            }

            var fitYanitlar = fitIrsaliye.IrsaliyeYanitiIndir(EVRAKGUID);
            if (fitYanitlar.Response.Length > 0)
            {
                if (fitYanitlar.Response[0].Receipts != null)
                {
                    foreach (var fitYanit in fitYanitlar.Response[0].Receipts)
                    {
                        var recData = fitYanit.DocData;

                        XmlSerializer ser = new XmlSerializer(typeof(ReceiptAdviceType));
                        ReceiptAdviceType receipt = (ReceiptAdviceType)ser.Deserialize(new MemoryStream(ZipUtility.UncompressFile(recData)));

                        var rejected = receipt.ReceiptLine.Any(elm => elm.RejectedQuantity?.Value != null);

                        if (rejected)
                        {
                            var Result = new Results.EFAGDN
                            {
                                DurumAciklama = receipt.ReceiptLine.FirstOrDefault(elm => elm.RejectedQuantity?.Value != null).RejectReason?[0]?.Value ?? "",
                                DurumKod = "3",
                                DurumZaman = receipt.IssueDate.Value,
                                UUID = fitYanitlar.Response[0].DespatchUUID,
                            };

                            Entegrasyon.UpdateEfagdnStatus(Result);
                        }
                    }
                }
            }
            break;

        }

        public override string AlinanFaturalarListesi()
        {

            base.AlinanFaturalarListesi();
            var data = new List<AlinanBelge>();

            var fitIrsaliye = new FIT.DespatchWebService();
            fitIrsaliye.WebServisAdresDegistir();

            for (DateTime dt = StartDate.Date; dt < EndDate.Date.AddDays(1); dt = dt.AddDays(1))
            {
                var fitResult = fitIrsaliye.GonderilenIrsaliyeler(dt);

                foreach (var irsaliye in fitResult)
                {
                    data.Add(new AlinanBelge
                    {
                        EVRAKGUID = Guid.Parse(irsaliye.UUID),
                        EVRAKNO = irsaliye.ID,
                        YUKLEMEZAMAN = irsaliye.InsertDateTime,
                        GBETIKET = "",
                        GBUNVAN = ""
                    });
                }
            }
            break;
            return data;
        }

        public override string Indir()
        {

            base.Indir();
            var Result = new Results.EFAGLN();

            var fitIrsaliye = new FIT.DespatchWebService();
            fitIrsaliye.WebServisAdresDegistir();

            var list = new List<string>();
            List<GetDesUBLListResponseType> fitGelen = new List<GetDesUBLListResponseType>();
            for (; day1.Date <= day2.Date; day1 = day1.AddDays(1))
            {
                Connector.m.IssueDate = new DateTime(day1.Year, day1.Month, day1.Day, 0, 0, 0);
                Connector.m.EndDate = new DateTime(day1.Year, day1.Month, day1.Day, 23, 59, 59);

                var gond = fitIrsaliye.GelenIrsaliyeler();
                if (gond.Response != null)
                {
                    if (gond.Response.Length > 0)
                    {
                        foreach (var fat in gond.Response)
                        {
                            list.Add(fat.UUID);
                            fitGelen.Add(fat);
                        }
                    }
                }
            }

            var lists = list.Split(20);

            foreach (var l in lists)
            {
                var ubls = fitIrsaliye.GelenIrsaliyeUBLIndir(l.ToArray());
                foreach (var ubl in ubls.Response)
                {
                    GetDesUBLListResponseType gln = null;
                    foreach (var g in fitGelen)
                    {
                        if (g.UUID == ubl.UUID)
                            gln = g;
                    }

                    Result.DurumAciklama = "";
                    Result.DurumKod = "";
                    Result.DurumZaman = gln.InsertDateTime;
                    Result.Etiket = gln.Identifier;
                    Result.EvrakNo = gln.ID;
                    Result.UUID = gln.UUID;
                    Result.VergiHesapNo = gln.VKN_TCKN;
                    Result.ZarfUUID = gln.EnvUUID.ToString();

                    Entegrasyon.InsertIrsaliye(Result, ZipUtility.UncompressFile(ubl.DocData));
                }
            }
            break;

        }

        public override string Kabul()
        {

            base.Kabul();
            var dosya = Entegrasyon.GelenDosya(GUID);

            XmlSerializer serializer = new XmlSerializer(typeof(UblDespatchAdvice.DespatchAdviceType));
            var desp = (UblDespatchAdvice.DespatchAdviceType)serializer.Deserialize(new MemoryStream(dosya));

            var yanit = new IrsaliyeYanitiUBL();
            yanit.CreateReceiptAdvice(desp, Aciklama);

            foreach (var iy in desp.DespatchLine)
                yanit.AddReceiptLine(iy, iy.DeliveredQuantity.Value, 0, 0, 0);

            yanit.GetYanit().ID = new UblReceiptAdvice.IDType { Value = Entegrasyon.GetIrsaliyeYanitEvrakNo() };

            var fitIrsaliye = new FIT.DespatchWebService();
            fitIrsaliye.WebServisAdresDegistir();

            var res = fitIrsaliye.IrsaliyeYanitiGonder(yanit.GetYanit());
            var fitDosya = fitIrsaliye.GonderilenIrsaliyeYanitlari(res.Response[0].EnvUUID, res.Response[0].UUID);
            var fitYnt = Entegrasyon.ConvertToYanit(fitDosya, "GDN", res.Response[0].EnvUUID);
            Entegrasyon.InsertIntoEirYnt(fitYnt);

            Entegrasyon.UpdateEfaglnStatus(new Results.EFAGLN { ZarfUUID = res.Response[0].EnvUUID }, GUID, true, Aciklama);
            break;

        }

        public override string Red()
        {

            base.Red();
            var dosya = Entegrasyon.GelenDosya(GUID);

            XmlSerializer serializer = new XmlSerializer(typeof(UblDespatchAdvice.DespatchAdviceType));
            var desp = (UblDespatchAdvice.DespatchAdviceType)serializer.Deserialize(new MemoryStream(dosya));

            var yanit = new IrsaliyeYanitiUBL();
            yanit.CreateReceiptAdvice(desp, Aciklama);

            foreach (var iy in desp.DespatchLine)
                yanit.AddReceiptLine(iy, 0, iy.DeliveredQuantity.Value, 0, 0);

            yanit.GetYanit().ID = new UblReceiptAdvice.IDType { Value = Entegrasyon.GetIrsaliyeYanitEvrakNo() };

            var fitIrsaliye = new FIT.DespatchWebService();
            fitIrsaliye.WebServisAdresDegistir();

            var res = fitIrsaliye.IrsaliyeYanitiGonder(yanit.GetYanit());
            var fitDosya = fitIrsaliye.GonderilenIrsaliyeYanitlari(res.Response[0].EnvUUID, res.Response[0].UUID);
            var fitYnt = Entegrasyon.ConvertToYanit(fitDosya, "GDN", res.Response[0].EnvUUID);
            Entegrasyon.InsertIntoEirYnt(fitYnt);

            Entegrasyon.UpdateEfaglnStatus(new Results.EFAGLN { ZarfUUID = res.Response[0].EnvUUID }, GUID, true, Aciklama);
            break;

        }

        public override string GonderilenGuncelleByDate()
        {

            base.GonderilenGuncelleByDate();
            var Result = new Results.EFAGDN();

            var fitIrsaliye = new FIT.DespatchWebService();
            fitIrsaliye.WebServisAdresDegistir();

            for (; start.Date <= end.Date; start = start.AddDays(1))
            {
                var evraklar = Entegrasyon.GidenIrsaliyeGUIDList(start, Connector.m.GbEtiketi);

                foreach (var evrak in evraklar.Where(elm => !string.IsNullOrEmpty(elm.Item1)))
                {
                    try
                    {
                        var fitGonderilenler = fitIrsaliye.IrsaliyeYanitiIndir(evrak.Item1);

                        foreach (var yanit in fitGonderilenler.Response)
                        {
                            if (yanit.Receipts == null)
                                continue;

                            var rcp = Entegrasyon.ConvertToYanitList(yanit, "GLN", evrak.Item2);
                            foreach (var r in rcp)
                                Entegrasyon.InsertIntoEirYnt(r);
                        }
                    }
                    catch (Exception) { }
                }
                //var yanitlar = fitIrsaliye.GelenEIrsaliyeYanit(start, end);
                //if (yanitlar.ServiceResult == COMMON.dpDespatch.Result.Error)
                //    throw new Exception(yanitlar.ServiceResultDescription);

                //foreach (var Receipment in yanitlar.Receipments.Where(elm => elm.Direction == COMMON.dpDespatch.Direction.Incoming))
                //{
                //    var rcp = Entegrasyon.ConvertToYanit(dpIrsaliye.GelenEIrsaliyeYanit(Receipment.UUID).Receipments[0], "GLN");
                //    rcp.REFEVRAKGUID = Receipment.DespatchUUID;
                //    rcp.REFEVRAKNO = Receipment.DespatchId;
                //    Entegrasyon.InsertIntoEirYnt(rcp);
                //}
            }
            break;

        }

        public override string GonderilenGuncelle()
        {



            base.GonderilenGuncelle();
            var Result = new Results.EFAGDN();

            var fitIrsaliye = new FIT.DespatchWebService();
            fitIrsaliye.WebServisAdresDegistir();

            var fitIRsaliyeler = fitIrsaliye.GidenIrsaliyeUBLIndir(UUIDs.ToArray());

            foreach (var f in fitIRsaliyeler.Response)
                Entegrasyon.UpdateEfadosIrsaliye(ZipUtility.UncompressFile(f.DocData));

            break;

        }


    }
}
