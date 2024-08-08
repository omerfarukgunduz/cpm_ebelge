namespace cpm_ebelge.Models
{

    public class eIrsaliyeProtectedValues
    {
        public dynamic strFatura { get; set; }
        public dynamic doc { get; set; }

    }
    public abstract class EIrsaliye : EIrsaliye_EFatura
    {
        protected eIrsaliyeProtectedValues eIrsaliyeProtectedValues { get; set; } = new eIrsaliyeProtectedValues();

        public static string TopluGonder(string PK, List<DespatchAdviceType> Irsaliyeler, string ENTSABLON)
        {
            {
                StringBuilder sb = new StringBuilder();
                var Result = new Results.EFAGDN();

                if (Connector.m.SchematronKontrol)
                {
                    DespatchAdviceSerializer ser = new DespatchAdviceSerializer();
                    foreach (var Irsaliye in Irsaliyeler)
                    {
                        var schematronResult = SchematronChecker.Check(Irsaliye, SchematronDocType.eIrsaliye);
                        if (schematronResult.SchemaResult != "Başarılı" || schematronResult.SchematronResult != "Başarılı")
                            throw new Exception(schematronResult.Detail);
                    }
                }

            }


            ///TOPLUGONDER---SWITCHE KADAR OLAN KISIM İÇİNDE OLUCAK DİĞERLERİ SEALED CLASSTA-----------------------------------------------------------------------------------------------------

        }
        public sealed class FIT_ING_INGBANKIrsaliye : EIrsaliye
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

        }
        public sealed class DPlanetEIrsaliye : EIrsaliye
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
        }
        public sealed class EDMEIrsaliye : EIrsaliye
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
        }
        public sealed class QEFEIrsaliye : EIrsaliye
        {
            public override string TopluGonder()
            {
                base.TopluGonder();
                var qefIrsaliye = new QEF.DespatchAdviceService();

                foreach (Irsaliye in Irsaliyeler)
                {
                    int EVRAKSN = Convert.ToInt32(Irsaliye.AdditionalDocumentReference.Where(element => element.DocumentTypeCode.Value == "CUST_DES_ID").First().ID.Value);
                    DespatchAdviceSerializer qefSerializer = new DespatchAdviceSerializer();
                    eIrsaliyeProtectedValues.strFatura = qefSerializer.GetXmlAsString(Irsaliye);

                    var qefResult = qefIrsaliye.IrsaliyeGonder(eIrsaliyeProtectedValues.strFatura, EVRAKSN, Irsaliye.IssueDate.Value.Date); ;

                    var qefFaturaUBL = qefIrsaliye.IrsaliyeUBLIndir(new[] { Irsaliye.UUID.Value });

                    Result.DurumAciklama = qefResult.aciklama ?? "";
                    Result.DurumKod = qefResult.gonderimDurumu.ToString();
                    Result.DurumZaman = DateTime.TryParse(qefResult.gonderimTarihi, out DateTime dz) ? dz : new DateTime(1900, 1, 1);
                    Result.EvrakNo = qefResult.belgeNo;
                    Result.UUID = qefResult.ettn;
                    Result.ZarfUUID = "";

                    Entegrasyon.UpdateEirsaliye(Result, EVRAKSN, qefFaturaUBL.First().Value);
                    sb.AppendLine("e-İrsaliye başarıyla gönderildi. \nEvrak No: " + Result.EvrakNo);
                }
                return sb.ToString();

            }
        }

        public static string Gonder(int EVRAKSN)
        {
            DespatchAdvice despatchAdvice = new DespatchAdvice();
            eIrsaliyeProtectedValues.doc = despatchAdvice.CreateDespactAdvice(EVRAKSN);

            if (UrlModel.SelectedItem == "QEF" && string.IsNullOrEmpty(eIrsaliyeProtectedValues.doc.ID.Value) && Connector.m.SablonTip)
            {
                var sablon = string.IsNullOrEmpty(despatchAdvice.ENTSABLON) ? Connector.m.Sablon : despatchAdvice.ENTSABLON;
                var notes = eIrsaliyeProtectedValues.doc.Note.ToList();
                notes.Add(new UblDespatchAdvice.NoteType { Value = $"#EFN_SERINO_TERCIHI#{sablon}#" });

                eIrsaliyeProtectedValues.doc.Note = notes.ToArray();
            }

            if (Connector.m.SchematronKontrol)
            {
                schematronResult = SchematronChecker.Check(eIrsaliyeProtectedValues.doc, SchematronDocType.eIrsaliye);
                if (schematronResult.SchemaResult != "Başarılı" || schematronResult.SchematronResult != "Başarılı")
                    throw new Exception(schematronResult.Detail);
            }

            UBLBaseSerializer serializer = new DespatchAdviceSerializer();
            eIrsaliyeProtectedValues.strFatura = serializer.GetXmlAsString(eIrsaliyeProtectedValues.doc);

            Connector.m.PkEtiketi = despatchAdvice.PK;
            Entegrasyon.EfagdnUuid(EVRAKSN, eIrsaliyeProtectedValues.doc.UUID.Value);


            var Result = new Results.EFAGDN();

        }
        public sealed class FIT_ING_INGBANKIrsaliye : EIrsaliye
        {
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
        }
        public sealed class DPlanetEIrsaliye : EIrsaliye
        {
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
        }
        public sealed class EDMEIrsaliye : EIrsaliye
        {
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
        }
        public sealed class QEFEIrsaliye : EIrsaliye
        {
            public override string TopluGonder()
            {
                base.TopluGonder();
                var qefIrsaliye = new QEF.DespatchAdviceService();

                var qefResult = qefIrsaliye.IrsaliyeGonder(eIrsaliyeProtectedValues.strFatura, EVRAKSN, eIrsaliyeProtectedValues.doc.IssueDate.Value.Date);

                if (!string.IsNullOrEmpty(qefResult.aciklama))
                    throw new Exception(qefResult.aciklama);

                var qefFaturaUBL = qefIrsaliye.IrsaliyeUBLIndir(new[] { eIrsaliyeProtectedValues.eIrsaliyeProtectedValues.doc.UUID.Value });

                Result.DurumAciklama = qefResult.aciklama ?? "";
                Result.DurumKod = qefResult.gonderimDurumu.ToString();
                Result.DurumZaman = DateTime.TryParse(qefResult.gonderimTarihi, out DateTime dz) ? dz : new DateTime(1900, 1, 1);
                Result.EvrakNo = qefResult.belgeNo;
                Result.UUID = qefResult.ettn;
                Result.ZarfUUID = "";

                Entegrasyon.UpdateEirsaliye(Result, EVRAKSN, qefFaturaUBL.First().Value);
                return "e-İrsaliye başarıyla gönderildi. \nEvrak No: " + Result.EvrakNo;

            }
        }
        public static void Esle(string GUID)
        {

        }
        public sealed class FIT_ING_INGBANKIrsaliye : EIrsaliye
        {
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
        }
        public sealed class DPlanetEIrsaliye : EIrsaliye
        {
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
        }
        public sealed class EDMEIrsaliye : EIrsaliye
        {
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
        }
        public sealed class QEFEIrsaliye : EIrsaliye
        {
            public override string Esle()
            {
                base.Esle();
                var Result = new Results.EFAGDN();
                var qefIrsaliye = new QEF.DespatchAdviceService();

                var qefGonderilenler = qefIrsaliye.GonderilenEIrsaliyeIndir(new[] { GUID });

                foreach (var doc in qefGonderilenler)
                {

                    Result.DurumAciklama = doc.Key.gonderimCevabiDetayi ?? "";
                    Result.DurumKod = doc.Key.gonderimCevabiKodu + "";
                    Result.DurumZaman = DateTime.TryParseExact(doc.Key.alimTarihi, "", CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dt) ? dt : new DateTime(1900, 1, 1);
                    Result.EvrakNo = doc.Key.belgeNo;
                    Result.UUID = doc.Key.ettn;
                    Result.ZarfUUID = "";

                    Entegrasyon.UpdateEfagdnStatus(Result);
                }
                break;

            }
        }
        public static void Esle(int EVRAKSN, DateTime GonderimTarih, string EVRAKGUID)

        {

        }
        public sealed class FIT_ING_INGBANKIrsaliye : EIrsaliye
        {
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
        }
        public sealed class DPlanetEIrsaliye : EIrsaliye
        {
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
        }
        public sealed class EDMEIrsaliye : EIrsaliye
        {
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
        }
        public sealed class QEFEIrsaliye : EIrsaliye
        {
            public override string Esle()
            {
                base.Esle();
                var Result = new Results.EFAGDN();
                var start = new DateTime(GonderimTarih.Year, GonderimTarih.Month, GonderimTarih.Day, 0, 0, 0);
                var end = new DateTime(GonderimTarih.Year, GonderimTarih.Month, GonderimTarih.Day, 23, 59, 59);
                var qefIrsaliye = new QEF.DespatchAdviceService();

                var qefGonderilenler = qefIrsaliye.GonderilenIrsaliyeler(start, end);

                foreach (var doc in qefGonderilenler)
                {
                    if (doc.yerelBelgeNo == EVRAKSN.ToString())
                    {
                        Result.DurumAciklama = doc.hataMesaji ?? "";
                        Result.DurumKod = "";
                        Result.DurumZaman = DateTime.TryParseExact(doc.alimZamani, "yyyyMMdd", CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dt) ? dt : new DateTime(1900, 1, 1);
                        Result.UUID = doc.ettn;

                        Entegrasyon.UpdateEIrsaliye(Result);
                        break;
                    }
                }
                break;
            }
        }
        public static List<AlinanBelge> AlinanFaturalarListesi(DateTime StartDate, DateTime EndDate)
        {

        }
        public sealed class FIT_ING_INGBANKIrsaliye : EIrsaliye
        {
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
        }
        public sealed class DPlanetEIrsaliye : EIrsaliye
        {
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
        }
        public sealed class EDMEIrsaliye : EIrsaliye
        {
            public override string AlinanFaturalarListesi()
            {
                base.AlinanFaturalarListesi();
                var data = new List<AlinanBelge>();
                return data;

            }
        }
        public sealed class QEFEIrsaliye : EIrsaliye
        {
            public override string AlinanFaturalarListesi()
            {
                base.AlinanFaturalarListesi();
                var data = new List<AlinanBelge>();

                var qefIrsaliye = new QEF.GetDespatchAdviceService();

                foreach (var fatura in qefIrsaliye.GelenEIrsaliyeler(StartDate, EndDate))
                {
                    data.Add(new AlinanBelge
                    {
                        EVRAKGUID = Guid.Parse(fatura.Value.ettn),
                        EVRAKNO = fatura.Value.ettn,
                        YUKLEMEZAMAN = DateTime.ParseExact(fatura.Value.alimTarihi, "yyyyMMdd", CultureInfo.CurrentCulture),
                        GBETIKET = "",
                        GBUNVAN = ""
                    });
                }
                break;
                return data;
            }
        }
        public static void Indir(DateTime day1, DateTime day2)
        {
        }
        public sealed class FIT_ING_INGBANKIrsaliye : EIrsaliye
        {
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
        }
        public sealed class DPlanetEIrsaliye : EIrsaliye
        {
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
        }
        public sealed class EDMEIrsaliye : EIrsaliye
        {
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
        }
        public sealed class QEFEIrsaliye : EIrsaliye
        {
            public override string Indir()
            {
                base.Indir();
                var Result = new Results.EFAGLN();

                var qefFatura = new QEF.GetDespatchAdviceService();
                foreach (var fatura in qefFatura.GelenEIrsaliyeler(day1, day2))
                {
                    Result.DurumAciklama = fatura.Value.yanitGonderimCevabiDetayi ?? "";
                    Result.DurumKod = fatura.Value.yanitGonderimCevabiKodu + "";
                    Result.DurumZaman = DateTime.ParseExact(fatura.Key.belgeTarihi, "yyyyMMdd", CultureInfo.CurrentCulture);
                    Result.Etiket = fatura.Key.gonderenEtiket;
                    Result.EvrakNo = fatura.Value.belgeNo;
                    Result.UUID = fatura.Value.ettn;
                    Result.VergiHesapNo = fatura.Key.gonderenVknTckn;
                    Result.ZarfUUID = "";

                    var ubl = ZipUtility.UncompressFile(qefFatura.GelenUBLIndir(fatura.Value.ettn));
                    Entegrasyon.InsertIrsaliye(Result, ubl);
                }
                break;
            }
        }

        public static void Kabul(string GUID, string Aciklama)
        {

        }
        public sealed class FIT_ING_INGBANKIrsaliye : EIrsaliye
        {
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
        }
        public sealed class DPlanetEIrsaliye : EIrsaliye
        {
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
        }
        public sealed class EDMEIrsaliye : EIrsaliye
        {
            public override string Kabul()
            {
                base.Kabul();
                throw new Exception("Entegratör Bu Eylemi Desteklememektedir!");

            }
        }
        public sealed class QEFEIrsaliye : EIrsaliye
        {
            public override string Kabul()
            {
                base.Kabul();
                throw new Exception("Entegratör Bu Eylemi Desteklememektedir!");
            }
        }
        public static void Red(string GUID, string Aciklama)
        {
        }
        public sealed class FIT_ING_INGBANKIrsaliye : EIrsaliye
        {
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
        }
        public sealed class DPlanetEIrsaliye : EIrsaliye
        {
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
        }
        public sealed class EDMEIrsaliye : EIrsaliye
        {
            public override string Red()
            {
                base.Red();
                throw new Exception("Entegratör Bu Eylemi Desteklememektedir!");

            }
        }
        public sealed class QEFEIrsaliye : EIrsaliye
        {
            public override string Red()
            {
                base.Red();
                throw new Exception("Entegratör Bu Eylemi Desteklememektedir!");
            }
        }
        public static void GonderilenGuncelleByDate(DateTime start, DateTime end)
        {
        }
        public sealed class FIT_ING_INGBANKIrsaliye : EIrsaliye
        {
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
        }
        public sealed class DPlanetEIrsaliye : EIrsaliye
        {
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
        }
        public sealed class EDMEIrsaliye : EIrsaliye
        {
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
        }
        public sealed class QEFEIrsaliye : EIrsaliye
        {
            public override string GonderilenGuncelleByDate()
            {
                base.GonderilenGuncelleByDate();
                var Result = new Results.EFAGDN();

                var qefIrsaliye = new QEF.DespatchAdviceService();
                var qefGelen = qefIrsaliye.GonderilenIrsaliyeler(start, end);

                //var edmYanitlar = edmFatura.GelenUygulamaYanitlari(day1, day2);

                foreach (var fatura in qefGelen)
                {
                    if (fatura.alimDurumu == "İşlendi" && fatura.ettn != null)
                        Entegrasyon.UpdateEfagdnGonderimDurum(fatura.ettn, 3);
                    else if (fatura.alimDurumu == "İşleme Hatası" && fatura.ettn != null)
                        Entegrasyon.UpdateEfagdnGonderimDurum(fatura.ettn, 0);

                    if (fatura.ettn != null)
                    {
                        Result.DurumAciklama = fatura.hataMesaji ?? "";
                        Result.DurumKod = "";
                        Result.DurumZaman = DateTime.TryParseExact(fatura.alimZamani, "yyyyMMdd", CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dt) ? dt : new DateTime(1900, 1, 1);
                        Result.UUID = fatura.ettn;
                        Result.ZarfUUID = "";

                        Entegrasyon.UpdateEfagdnStatus(Result);
                    }
                }
                break;
            }
        }

        public static void GonderilenGuncelle(List<string> UUIDs)
        {
        }
        public sealed class FIT_ING_INGBANKIrsaliye : EIrsaliye
        {
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
        public sealed class DPlanetEIrsaliye : EIrsaliye
        {
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
        public sealed class EDMEIrsaliye : EIrsaliye
        {
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
        public sealed class QEFEIrsaliye : EIrsaliye
        {
            public override string GonderilenGuncelle()
            {
                base.GonderilenGuncelle();
                var Result = new Results.EFAGDN();

                var qefIrsaliye = new QEF.DespatchAdviceService();

                var qefDosyalar = qefIrsaliye.IrsaliyeUBLIndir(UUIDs.ToArray());

                foreach (var dosya in qefDosyalar)
                    Entegrasyon.UpdateEfadosIrsaliye(dosya.Value);
                break;
            }
        }
        /// SADECE EIRSALIYEDE OLANLAR 
        public static void YanitGonder(ReceiptAdviceType Yanit)
        {
            ReceiptAdviceSerializer serializer = new ReceiptAdviceSerializer();
            var docStr = serializer.GetXmlAsString(Yanit);
            eIrsaliyeProtectedValues.doc = Encoding.UTF8.GetBytes(docStr);
            switch (UrlModel.SelectedItem)
            {
                case "FIT":
                case "ING":
                case "INGBANK":
                    var fitIrsaliye = new FIT.DespatchWebService();
                    fitIrsaliye.WebServisAdresDegistir();

                    //var res = fitIrsaliye.IrsaliyeYanitiGonder(Yanit);
                    //var fitDosya = fitIrsaliye.IrsaliyeYanitiIndir(res.Response[0].UUID);
                    //var fitYnt = Entegrasyon.ConvertToYanit(fitDosya.Response[0], "GDN");
                    //
                    //Entegrasyon.InsertIntoEirYnt(fitYnt);

                    Yanit.ID = new UblReceiptAdvice.IDType { Value = Entegrasyon.GetIrsaliyeYanitEvrakNo() };

                    var res = fitIrsaliye.IrsaliyeYanitiGonder(Yanit);
                    var fitDosya = fitIrsaliye.GonderilenIrsaliyeYanitlari(res.Response[0].EnvUUID, res.Response[0].UUID);
                    var fitYnt = Entegrasyon.ConvertToYanit(fitDosya, "GDN", res.Response[0].EnvUUID);
                    Entegrasyon.InsertIntoEirYnt(fitYnt);
                    break;
                case "DPLANET":
                    var dpIrsaliye = new DigitalPlanet.DespatchWebService();
                    dpIrsaliye.WebServisAdresDegistir();
                    dpIrsaliye.Login();

                    var dpSonuc = dpIrsaliye.EIrsaliyeCevap(eIrsaliyeProtectedValues.doc);
                    //File.WriteAllText("ReceiptAdvice_Resp.json", JsonConvert.SerializeObject(dpSonuc));
                    if (dpSonuc.ServiceResult == COMMON.dpDespatch.Result.Error)
                        throw new Exception(dpSonuc.ServiceResultDescription);

                    foreach (var receipments in dpSonuc.Receipments)
                    {
                        var irsaliyeYanit = dpIrsaliye.GidenEIrsaliyeYanitIndir(receipments.UUID);
                        var ynt = Entegrasyon.ConvertToYanit(irsaliyeYanit.Receipments[0], "GDN");

                        Entegrasyon.InsertIntoEirYnt(ynt);
                    }
                    break;
                case "EDM":
                    break;
                case "QEF":
                    break;
                default:
                    throw new Exception("Tanımlı Entegratör Bulunamadı!");
            }
        }
        public static void YanitGuncelle(string UUID, string ZarfGuid)
        {
            switch (UrlModel.SelectedItem)
            {
                case "FIT":
                case "ING":
                case "INGBANK":
                    var fitIrsaliye = new FIT.DespatchWebService();
                    fitIrsaliye.WebServisAdresDegistir();
                    var fitDosya = fitIrsaliye.GonderilenIrsaliyeYanitlari(ZarfGuid, UUID);
                    var fitYnt = Entegrasyon.ConvertToYanit(fitDosya, "GDN", ZarfGuid);

                    if (appConfig.Debugging)
                    {
                        MessageBox.Show(ZarfGuid);
                    }
                    Entegrasyon.UpdateEirYnt(fitYnt);
                    break;
                case "DPLANET":
                    var dpIrsaliye = new DigitalPlanet.DespatchWebService();
                    dpIrsaliye.WebServisAdresDegistir();
                    dpIrsaliye.Login();

                    var dpSonuc = dpIrsaliye.EIrsaliyeYanitDurumu(UUID);
                    var durum = new EIRYNT
                    {
                        DURUMACIKLAMA = dpSonuc.StatusDescription,
                        DURUMKOD = dpSonuc.StatusCode == 54 ? "1300" : dpSonuc.StatusCode + "",
                        EVRAKGUID = UUID
                    };

                    Entegrasyon.UpdateEirYnt(durum);
                    break;
                case "EDM":
                    break;
                default:
                    throw new Exception("Tanımlı Entegratör Bulunamadı!");
            }
        }
        public static void GonderilenYanitlar(string UUID)
        {
            switch (UrlModel.SelectedItem)
            {
                case "ING":
                case "INGBANK":
                case "FIT":
                    var fitIrsaliye = new FIT.DespatchWebService();
                    fitIrsaliye.WebServisAdresDegistir();
                    var yanitlar = fitIrsaliye.IrsaliyeYanitiIndir(UUID);

                    var yanitlar2 = Entegrasyon.ConvertToYanitList(yanitlar.Response[0], "GDN", Entegrasyon.GetEvrakNoFromGuid(UUID));
                    if (appConfig.Debugging)
                    {
                        if (yanitlar?.Response?.Length > 0)
                        {
                            if (yanitlar?.Response[0]?.Receipts?.Length > 0)
                            {
                                MessageBox.Show(yanitlar.Response[0].Receipts[0].EnvUUID);
                            }
                        }
                    }
                    foreach (var yanit in yanitlar2)
                    {
                        Entegrasyon.InsertIntoEirYnt(yanit);
                        Entegrasyon.UpdateEirYnt(yanit);
                    }
                    break;
                case "DPLANET":
                    //var dpIrsaliye = new DigitalPlanet.DespatchWebService();
                    //dpIrsaliye.EIrsaliyeGonderilenYanitlar(UUID);
                    break;
                case "EDM":
                    break;
                default:
                    throw new Exception("Tanımlı Entegratör Bulunamadı!");
            }
        }
    } 
}
