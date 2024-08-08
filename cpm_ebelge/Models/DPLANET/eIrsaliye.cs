using System;
using System.Xml.Serialization;

namespace cpm_ebelge.Models.DPLANET
{
    public class eIrsaliye
    {


        public override string TopluGonder()
        {
            base.TopluGonder();
            var dpIrsaliye = new DigitalPlanet.DespatchWebService();
            dpIrsaliye.WebServisAdresDegistir();
            dpIrsaliye.Login();
            Connector.m.PkEtiketi = PK;
            var dpResult = dpIrsaliye.TopluEIrsaliyeGonder(Irsaliyeler, ENTSABLON);

            if (dpResult.ServiceResult == COMMON.dpDespatch.Result.Error)
                throw new Exception(dpResult.ServiceResultDescription);

            foreach (var doc in dpResult.Despatches)
            {
                var fat = dpIrsaliye.GonderilenEIrsaliyeIndir(doc.UUID);
                XmlSerializer serializer = new XmlSerializer(typeof(DespatchAdviceType));
                DespatchAdviceType inv = (DespatchAdviceType)serializer.Deserialize(new MemoryStream(fat.Despatches[0].ReturnValue));

                Result.DurumAciklama = fat.ServiceResultDescription;
                Result.DurumKod = fat.ServiceResult == COMMON.dpDespatch.Result.Successful ? "1" : dpResult.ErrorCode + "";
                Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
                Result.EvrakNo = fat.Despatches[0].DespatchId;
                Result.UUID = fat.Despatches[0].UUID;
                Result.ZarfUUID = dpResult.InstanceIdentifier;

                Entegrasyon.UpdateEirsaliye(Result, Convert.ToInt32(inv.AdditionalDocumentReference.Where(element => element.DocumentTypeCode.Value == "CUST_DES_ID").First().ID.Value), fat.Despatches[0].ReturnValue);
                sb.AppendLine("e-İrsaliye başarıyla gönderildi. \nEvrak No: " + fat.Despatches[0].DespatchId);
            }
            return sb.ToString();


        }

        public override string Gonder()
        {
            base.Gonder();
            var dpIrsaliye = new DigitalPlanet.DespatchWebService();
            dpIrsaliye.Login();

            var dpResult = dpIrsaliye.EIrsaliyeGonder(eIrsaliyeProtectedValues.strFatura, eIrsaliyeProtectedValues.doc.UUID.Value, eIrsaliyeProtectedValues.doc.IssueDate.Value, despatchAdvice.ENTSABLON);

            if (dpResult.ServiceResult == COMMON.dpDespatch.Result.Error)
                throw new Exception(dpResult.ErrorCode + ":" + dpResult.ServiceResultDescription);

            //var dpFaturaUBL = dpIrsaliye.GonderilenEIrsaliyeIndir(dpResult.Despatches[0].UUID);

            Result.DurumAciklama = dpResult.ServiceResultDescription;
            Result.DurumKod = dpResult.ServiceResult == COMMON.dpDespatch.Result.Successful ? "1" : dpResult.ErrorCode.ToString();
            Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
            Result.EvrakNo = dpResult.Despatches[0].DespatchId;
            Result.UUID = dpResult.Despatches[0].UUID;
            Result.ZarfUUID = dpResult.InstanceIdentifier;

            //Entegrasyon.UpdateEirsaliye(Result, EVRAKSN, dpFaturaUBL.Despatches[0].ReturnValue);
            Entegrasyon.UpdateEirsaliye(Result, EVRAKSN, null);
            if (Connector.m.DokumanIndir)
            {
                var Gonderilen = dpIrsaliye.GonderilenEIrsaliyeIndir(Result.UUID);
                Entegrasyon.UpdateEfadosIrsaliye(Gonderilen.Despatches[0].ReturnValue);
            }
            return "e-İrsaliye başarıyla gönderildi. \nEvrak No: " + dpResult.Despatches[0].DespatchId;

        }

        public override string Esle()
        {
            base.Esle();
            var Result = new Results.EFAGDN();
            var dpIrsaliye = new DigitalPlanet.DespatchWebService();
            dpIrsaliye.WebServisAdresDegistir();
            dpIrsaliye.Login();

            COMMON.dpDespatch.DespatchPackResult yanit = null;
            string errorResult = "";

            try
            {
                yanit = dpIrsaliye.GonderilenEIrsaliyeIndir(GUID.ToLower());
                if (yanit.ServiceResult == COMMON.dpDespatch.Result.Error)
                {
                    errorResult = yanit.ServiceResultDescription;
                    yanit = null;
                }
            }
            catch (Exception ex)
            {
                if (appConfig.Debugging)
                    appConfig.DebuggingException(ex);

                errorResult = ex.Message;
                yanit = null;
            }

            try
            {
                if (yanit == null)
                {
                    yanit = dpIrsaliye.GonderilenEIrsaliyeIndir(GUID.ToUpper());
                    if (yanit.ServiceResult == COMMON.dpDespatch.Result.Error)
                    {
                        errorResult = yanit.ServiceResultDescription;
                        yanit = null;
                    }
                }
            }
            catch (Exception ex)
            {
                if (appConfig.Debugging)
                    appConfig.DebuggingException(ex);

                errorResult = ex.Message;
                yanit = null;
            }

            if (yanit != null)
            {
                foreach (var ynt in yanit.Despatches)
                {
                    Result.DurumAciklama = ynt.StatusDescription;
                    Result.DurumKod = ynt.StatusCode + "";
                    Result.DurumZaman = ynt.Issuetime;
                    Result.EvrakNo = ynt.DespatchId;
                    Result.UUID = ynt.UUID;
                    Result.ZarfUUID = "";

                    //var fat = dpIrsaliye.GonderilenEIrsaliyeIndir(ynt.UUID);
                    //var fatBytes = fat.Despatches[0].ReturnValue;

                    Entegrasyon.UpdateEIrsaliye(Result);

                    var yanitlar = dpIrsaliye.GelenEIrsaliyeYanitByIsaliyeNo(ynt.UUID);
                    if (yanitlar.ServiceResult == COMMON.dpDespatch.Result.Error)
                    {
                        errorResult = yanit.ServiceResultDescription;
                        yanit = null;
                    }

                    foreach (var Receipment in yanitlar.Receipments)
                    {
                        var rcp = Entegrasyon.ConvertToYanit(Receipment, "GLN");
                        Entegrasyon.InsertIntoEirYnt(rcp);
                    }
                }
            }
            else if (errorResult != "" && yanit == null)
                throw new Exception(errorResult);
            break;
        }

        public override string Esle()
        {
            base.Esle();
            var Result = new Results.EFAGDN();
            var start = new DateTime(GonderimTarih.Year, GonderimTarih.Month, GonderimTarih.Day, 0, 0, 0);
            var end = new DateTime(GonderimTarih.Year, GonderimTarih.Month, GonderimTarih.Day, 23, 59, 59);
            var dpIrsaliye = new DigitalPlanet.DespatchWebService();
            dpIrsaliye.WebServisAdresDegistir();
            dpIrsaliye.Login();

            var yanit = dpIrsaliye.GonderilenEIrsaliyeler(start, end);

            if (yanit.ServiceResult == COMMON.dpDespatch.Result.Error)
                throw new Exception(yanit.ServiceResultDescription);
            else
            {
                foreach (var ynt in yanit.Despatches)
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(DespatchAdviceType));
                    var irsData = dpIrsaliye.GonderilenEIrsaliyeIndir(ynt.UUID);
                    var irs = (DespatchAdviceType)serializer.Deserialize(new MemoryStream(irsData.Despatches[0].ReturnValue));

                    var custInv = irs.AdditionalDocumentReference.Where(elm => elm.DocumentTypeCode.Value == "CUST_DES_ID");
                    if (custInv.Any())
                    {
                        if (custInv.First().ID.Value == EVRAKSN + "")
                        {
                            Result.DurumAciklama = ynt.StatusDescription;
                            Result.DurumKod = ynt.StatusCode + "";
                            Result.DurumZaman = ynt.Issuetime;
                            Result.EvrakNo = ynt.DespatchId;
                            Result.UUID = ynt.UUID;
                            Result.ZarfUUID = "";

                            ///Entegrasyon.UpdateEfagdnStatus(Result);
                            Entegrasyon.UpdateEIrsaliye(Result);
                            break;
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

            var dpIrsaliye = new DigitalPlanet.DespatchWebService();
            dpIrsaliye.WebServisAdresDegistir();
            dpIrsaliye.Login();

            Connector.m.IssueDate = StartDate;
            Connector.m.EndDate = EndDate;

            foreach (var irsaliye in dpIrsaliye.GelenEIrsaliyeler().Despatches)
            {
                if (irsaliye.Issuetime > StartDate)
                    data.Add(new AlinanBelge
                    {
                        EVRAKGUID = Guid.Parse(irsaliye.UUID),
                        EVRAKNO = irsaliye.DespatchId,
                        YUKLEMEZAMAN = irsaliye.Issuetime,
                        GBETIKET = irsaliye.SenderPostBoxName,
                        GBUNVAN = irsaliye.Partyname
                    });
            }
            break;
            return data;

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
            var Result = new Results.EFAGLN();

            var dpIrsaliye = new DigitalPlanet.DespatchWebService();
            dpIrsaliye.Login();
            Connector.m.IssueDate = new DateTime(day1.Year, day1.Month, day1.Day, 0, 0, 0);
            Connector.m.EndDate = new DateTime(day2.Year, day2.Month, day2.Day, 23, 59, 59);

            var dpGelen = dpIrsaliye.GelenEIrsaliyeler();
            if (dpGelen.Despatches.Count() > 0)
            {
                foreach (var ubl in dpGelen.Despatches)
                {
                    Connector.m.GbEtiketi = ubl.SenderPostBoxName;
                    eIrsaliyeProtectedValues.doc = dpIrsaliye.GelenEIrsaliyeIndir(ubl.UUID);

                    Result.DurumAciklama = ubl.StatusDescription;
                    Result.DurumKod = ubl.StatusCode + "";
                    Result.DurumZaman = ubl.Issuetime;
                    Result.Etiket = ubl.SenderPostBoxName;
                    Result.EvrakNo = ubl.DespatchId;
                    Result.UUID = ubl.UUID;
                    Result.VergiHesapNo = ubl.Sendertaxid;
                    Result.ZarfUUID = "";

                    Entegrasyon.InsertIrsaliye(Result, eIrsaliyeProtectedValues.doc.Despatches[0].ReturnValue);
                }
            }
            break;
        }

        public override string Kabul()
        {
            base.Kabul();
            var dpIrsaliye = new DigitalPlanet.DespatchWebService();
            dpIrsaliye.WebServisAdresDegistir();

            var result = dpIrsaliye.EIrsaliyeCevap(GUID, true, Aciklama);

            if (result.ServiceResult == COMMON.dpDespatch.Result.Error)
                throw new Exception(result.ServiceResultDescription);

            var irsaliyeYanit = dpIrsaliye.GidenEIrsaliyeYanitIndir(result.Receipments[0].UUID);
            var ynt = Entegrasyon.ConvertToYanit(irsaliyeYanit.Receipments[0], "GDN");

            Entegrasyon.InsertIntoEirYnt(ynt);

            Entegrasyon.UpdateEfaglnStatus(new Results.EFAGLN { ZarfUUID = result.Receipments[0].ReceiptmentId }, GUID, true, Aciklama);
            break;

        }

        public override string Red()
        {
            base.Red();
            var dpIrsaliye = new DigitalPlanet.DespatchWebService();
            dpIrsaliye.WebServisAdresDegistir();

            var result = dpIrsaliye.EIrsaliyeCevap(GUID, false, Aciklama);

            if (result.ServiceResult == COMMON.dpDespatch.Result.Error)
                throw new Exception(result.ServiceResultDescription);


            var irsaliyeYanit = dpIrsaliye.GidenEIrsaliyeYanitIndir(result.Receipments[0].UUID);
            var ynt = Entegrasyon.ConvertToYanit(irsaliyeYanit.Receipments[0], "GDN");

            Entegrasyon.InsertIntoEirYnt(ynt);

            Entegrasyon.UpdateEfaglnStatus(new Results.EFAGLN { ZarfUUID = result.Receipments[0].ReceiptmentId }, GUID, false, Aciklama);
            break;

        }


        public override string GonderilenGuncelleByDate()
        {
            base.GonderilenGuncelleByDate();
            var Result = new Results.EFAGDN();

            var dpIrsaliye = new DigitalPlanet.DespatchWebService();
            dpIrsaliye.WebServisAdresDegistir();
            dpIrsaliye.Login();

            /*
            var yanit = dpIrsaliye.EIrsaliyeDurumSorgula(start, end);
            //var gonderilenler = dpIrsaliye.GonderilenEIrsaliyeler(start, end);
            if (yanit.ServiceResult == COMMON.dpDespatch.Result.Error)
                throw new Exception(yanit.ServiceResultDescription);

            foreach (var ynt in yanit.Despatches)
            {
                Result.DurumAciklama = ynt.StatusDescription;
                Result.DurumKod = ynt.StatusCode + "";
                Result.DurumZaman = ynt.Issuetime;
                Result.EvrakNo = ynt.DespatchId;
                Result.UUID = ynt.UUID;
                Result.ZarfUUID = "";

                var fat = dpIrsaliye.GonderilenEIrsaliyeIndir(ynt.UUID);
                var fatBytes = fat.Despatches[0].ReturnValue;

                ///Entegrasyon.UpdateEfagdnStatus(Result);
                Entegrasyon.UpdateEIrsaliye(Result, fatBytes);
            }
            */

            var yanitlar = dpIrsaliye.GelenEIrsaliyeYanit(start, end);
            if (yanitlar.ServiceResult == COMMON.dpDespatch.Result.Error)
                throw new Exception(yanitlar.ServiceResultDescription);

            foreach (var Receipment in yanitlar.Receipments.Where(elm => elm.Direction == COMMON.dpDespatch.Direction.Incoming))
            {
                var rcp = Entegrasyon.ConvertToYanit(dpIrsaliye.GelenEIrsaliyeYanit(Receipment.UUID).Receipments[0], "GLN");
                rcp.REFEVRAKGUID = Receipment.DespatchUUID;
                rcp.REFEVRAKNO = Receipment.DespatchId;
                Entegrasyon.InsertIntoEirYnt(rcp);
            }
            break;

        }

        public override string GonderilenGuncelle()
        {
            base.GonderilenGuncelle();
            var Result = new Results.EFAGDN();

            var dpIrsaliye = new DigitalPlanet.DespatchWebService();
            dpIrsaliye.WebServisAdresDegistir();
            dpIrsaliye.Login();

            List<byte[]> dosyalar = new List<byte[]>();
            foreach (string UUID in UUIDs)
            {
                var gond = dpIrsaliye.GonderilenEIrsaliyeIndir(UUID);
                dosyalar.Add(gond.Despatches[0].ReturnValue);
            }

            foreach (var dosya in dosyalar)
                Entegrasyon.UpdateEfadosIrsaliye(dosya);
            break;

        }


    }
}
