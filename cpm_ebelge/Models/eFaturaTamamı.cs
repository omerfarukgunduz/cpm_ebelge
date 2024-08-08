namespace cpm_ebelge.Models
{
    public class eFaturaProtectedValues
    {
        public string doc { get; set; }
        public bool resend { get; set; }
        public BaseUbl createdUBL { get; set; } // VAR TİPİNDEYDİ NE OLDUGUNU BİLMEDĞİMİZ İÇİN DYNAMIC TANIMLADIK
        public dynamic strFatura { get; set; } // VAR TİPİNDEYDİ NE OLDUGUNU BİLMEDĞİMİZ İÇİN DYNAMIC TANIMLADIK
    }

    public abstract class EFatura : EIrsaliye_EFatura
    {
        protected eFaturaProtectedValues eFaturaProtectedValues { get; set; } = new eFaturaProtectedValues();
        ///GONDER----------------------------------------------------------------------------------------------------
        public override string Gonder(int EVRAKSN)
        {
            DateTime dt = DateTime.Now;
            eFaturaProtectedValues.doc = GeneralCreator.GetUBLInvoiceData(EVRAKSN);
            eFaturaProtectedValues.resend = false;
            if (doc != null)
            {
                Connector.m.PkEtiketi = doc.PK;
                eFaturaProtectedValues.createdUBL = doc.BaseUBL;  // Fatura UBL i oluşturulur

                if (createdUBL.ProfileID.Value == "IHRACAT")
                    Connector.m.PkEtiketi = "urn:mail:ihracatpk@gtb.gov.tr";
                else
                    Connector.m.PkEtiketi = doc.PK;

                if (doc.OLDGUID == "")
                    Entegrasyon.EfagdnUuid(EVRAKSN, createdUBL.UUID.Value);
                else
                    eFaturaProtectedValues.resend = true;

                if (UrlModel.SelectedItem == "DPLANET")
                    eFaturaProtectedValues.createdUBL.ID = eFaturaProtectedValues.createdUBL.ID ?? new UblInvoiceObject.IDType { Value = "CPM" + DateTime.Now.Year + EVRAKSN.ToString("000000000") };

                if (UrlModel.SelectedItem == "QEF" && Connector.m.SablonTip)
                {
                    var sablon = string.IsNullOrEmpty(doc.ENTSABLON) ? Connector.m.Sablon : doc.ENTSABLON;

                    if (eFaturaProtectedValues.createdUBL.Note == null)
                        eFaturaProtectedValues.createdUBL.Note = new UblInvoiceObject.NoteType[0];

                    var list = createdUBL.Note.ToList(); //var list idi
                    list.Add(new UblInvoiceObject.NoteType { Value = $"#EFN_SERINO_TERCIHI#{sablon}#" }); ;
                    createdUBL.Note = list.ToArray();
                }
                //createdUBL.ID = createdUBL.ID ?? new UblInvoiceObject.IDType { Value = "GIB2022000000001 " };

                InvoiceSerializer serializer = new InvoiceSerializer(); // UBL  XML e dönüştürülür
                if (Connector.m.SchematronKontrol)
                {
                    schematronResult = SchematronChecker.Check(createdUBL, SchematronDocType.eFatura);
                    if (schematronResult.SchemaResult != "Başarılı" || schematronResult.SchematronResult != "Başarılı")
                        throw new Exception(schematronResult.Detail);
                }
                eFaturaProtectedValues.strFatura = serializer.GetXmlAsString(createdUBL); // XML byte tipinden string tipine dönüştürülür

                var Result = new Results.EFAGDN();
            }

        }
        public sealed class FIT_ING_INGBANKEFatura : EFatura
        {
            public override string Gonder()
            {
                base.Gonder();
                var fitEFatura = new FIT.InvoiceWebService();
                SendUBLResponseType[] fitResult;
                if (eFaturaProtectedValues.resend)
                    fitResult = fitEFatura.FaturaYenidenGonder(strFatura, createdUBL.UUID.Value, doc.OLDGUID);
                else
                    fitResult = fitEFatura.FaturaGonder(strFatura, eFaturaProtectedValues.createdUBL.UUID.Value);

                var envResult = fitEFatura.ZarfDurumSorgula(new[] { fitResult[0].EnvUUID });

                Result.DurumAciklama = envResult[0].Description;
                Result.DurumKod = envResult[0].ResponseCode;
                Result.DurumZaman = envResult[0].IssueDate;
                Result.EvrakNo = fitResult[0].ID;
                Result.UUID = fitResult[0].UUID;
                Result.ZarfUUID = envResult[0].UUID;
                Result.YanitDurum = doc.METHOD == "TEMELFATURA" ? 1 : 0;

                Entegrasyon.UpdateEfagdn(Result, EVRAKSN, null);
                if (Connector.m.DokumanIndir)
                {
                    var Gonderilen = fitEFatura.FaturaUBLIndir(new[] { Result.UUID });
                    Entegrasyon.UpdateEfados(Gonderilen[0]);
                }
                return "e-Fatura başarıyla gönderildi. \nEvrak No: " + fitResult[0].ID;
            }
        }
        public sealed class DPlanetEFatura : EFatura
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
        }
        public sealed class EDMEFatura : EFatura
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
        }
        public sealed class QEFEFatura : EFatura
        {
            public override string Gonder()
            {
                base.Gonder();
                var qefEFatura = new QEF.InvoiceService();
                strFatura = serializer.GetXmlAsString(createdUBL);
                var qefResult = qefEFatura.FaturaGonder(strFatura, EVRAKSN, createdUBL.IssueDate.Value);

                if (qefResult.durum == 2)
                    throw new Exception("Bir Hata Oluştu!\n" + qefResult.aciklama);

                Result.DurumAciklama = qefResult.aciklama ?? "";
                Result.DurumKod = qefResult.gonderimDurumu.ToString();
                Result.DurumZaman = DateTime.TryParse(qefResult.gonderimTarihi, out DateTime dz) ? dz : new DateTime(1900, 1, 1);
                Result.EvrakNo = qefResult.belgeNo;
                Result.UUID = qefResult.ettn;
                Result.ZarfUUID = "";
                Result.YanitDurum = doc.METHOD == "TEMELFATURA" ? 1 : 0;

                Entegrasyon.UpdateEfagdn(Result, EVRAKSN, null);
                if (Connector.m.DokumanIndir)
                {
                    var Gonderilen = qefEFatura.FaturaUBLIndir(new[] { Result.UUID });
                    Entegrasyon.UpdateEfados(Gonderilen.First().Value);
                }
                return "e-Fatura başarıyla gönderildi. \nEvrak No: " + qefResult.belgeNo;
            }
        }
        ///------------------------------------------------------------------------------------------------------------------
        ///TOPLUGONDER--------------------------------------------------------------------------------------------------------
        public override string TopluGonder(List<BaseInvoiceUBL> Faturalar, string ENTSABLON)
        {
            StringBuilder sb = new StringBuilder();
            UBLBaseSerializer serializer = new InvoiceSerializer(); // UBL  XML e dönüştürülür

            var Result = new Results.EFAGDN();

            List<InvoiceType> Faturalar2 = new List<InvoiceType>();

            foreach (var Fatura in Faturalar)
            {
                int EVRAKSN = Convert.ToInt32(Fatura.BaseUBL.AdditionalDocumentReference.FirstOrDefault(elm => elm.DocumentTypeCode.Value == "CUST_INV_ID")?.ID.Value ?? "0");

                if (UrlModel.SelectedItem == "DPLANET")
                    Fatura.BaseUBL.ID = Fatura.BaseUBL.ID ?? new UblInvoiceObject.IDType { Value = "CPM" + DateTime.Now.Year + EVRAKSN.ToString("000000000") };

                if (UrlModel.SelectedItem == "QEF" && Connector.m.SablonTip && Fatura?.BaseUBL != null && Fatura?.BaseUBL?.ID?.Value == "CPM" + DateTime.Now.Year + "000000001")
                {
                    var sablon = string.IsNullOrEmpty(Fatura.ENTSABLON) ? Connector.m.Sablon : Fatura.ENTSABLON;
                    var list = Fatura?.BaseUBL.Note.ToList();
                    list.Add(new UblInvoiceObject.NoteType { Value = $"#EFN_SERINO_TERCIHI#{sablon}#" });
                    Fatura.BaseUBL.Note = list.ToArray();
                }
                //Fatura.BaseUBL.ID = Fatura.BaseUBL.ID ?? new UblInvoiceObject.IDType { Value = "GIB2022000000001 " };

                Faturalar2.Add(Fatura.BaseUBL);
            }

            if (Connector.m.SchematronKontrol)
            {
                InvoiceSerializer ser = new InvoiceSerializer();
                foreach (var Fatura in Faturalar2)
                {
                    eFaturaProtectedValues.schematronResult = SchematronChecker.Check(Fatura, SchematronDocType.eFatura);
                    if (schematronResult.SchemaResult != "Başarılı" || schematronResult.SchematronResult != "Başarılı")
                        throw new Exception(schematronResult.Detail);
                }
            }

        }
        public sealed class FIT_ING_INGBANKEFatura : EFatura
        {
            public override string TopluGonder()
            {
                base.TopluGonder();
                var fitFatura = new FIT.InvoiceWebService();

                Dictionary<string, string> strFaturalar = new Dictionary<string, string>();
                Dictionary<string, string> Alici = new Dictionary<string, string>();

                foreach (var Fatura in Faturalar)
                {
                    var strFatura = serializer.GetXmlAsString(Fatura.BaseUBL); // XML byte tipinden string tipine dönüştürülür

                    strFaturalar.Add(Fatura.BaseUBL.UUID.Value, strFatura);
                }

                var strFats = strFaturalar.ToList();

                int i = 0;
                foreach (var Fatura in Faturalar)
                {
                    Connector.m.PkEtiketi = Fatura.PK;
                    fitFatura.WebServisAdresDegistir();
                    var result = fitFatura.FaturaGonder(strFats[i].Value, strFats[i].Key);

                    i++;

                    var envResult = fitFatura.ZarfDurumSorgula(new[] { result[0].EnvUUID });

                    foreach (var res in result)
                    {
                        Result.DurumAciklama = envResult[0].Description;
                        Result.DurumKod = envResult[0].ResponseCode;
                        Result.DurumZaman = envResult[0].IssueDate;
                        Result.EvrakNo = res.ID;
                        Result.UUID = res.UUID;
                        Result.ZarfUUID = res.EnvUUID;
                        Result.YanitDurum = Fatura.METHOD == "TEMELFATURA" ? 1 : 0;
                        Entegrasyon.UpdateEfagdn(Result, Convert.ToInt32(res.CustInvID), null);
                        if (Connector.m.DokumanIndir)
                        {
                            var Gonderilen = fitFatura.FaturaUBLIndir(new[] { Result.UUID });
                            Entegrasyon.UpdateEfados(Gonderilen[0]);
                        }
                    }

                    sb.AppendLine("e-Fatura başarıyla gönderildi. \nEvrak No: " + result[0].ID);
                }

                return sb.ToString();
            }
        }
        public sealed class DPlanetEFatura : EFatura
        {
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
        }
        public sealed class EDMEFatura : EFatura
        {
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
        }
        public sealed class QEFEFatura : EFatura
        {
            public override string TopluGonder()
            {
                base.TopluGonder();
                var qefFatura = new QEF.InvoiceService();

                foreach (var Fatura in Faturalar)
                {
                    if (Connector.m.SablonTip)
                    {
                        var createdUBL = Fatura.BaseUBL;
                        var sablon = string.IsNullOrEmpty(Fatura.ENTSABLON) ? Connector.m.Sablon : Fatura.ENTSABLON;

                        if (createdUBL.Note == null)
                            createdUBL.Note = new UblInvoiceObject.NoteType[0];

                        var list = createdUBL.Note.ToList();
                        list.Add(new UblInvoiceObject.NoteType { Value = $"#EFN_SERINO_TERCIHI#{sablon}#" }); ;
                        createdUBL.Note = list.ToArray();
                    }

                    if (Fatura.BaseUBL.ProfileID.Value == "IHRACAT")
                        Connector.m.PkEtiketi = "urn:mail:ihracatpk@gtb.gov.tr";
                    else
                        Connector.m.PkEtiketi = Fatura.PK;

                    var strFatura = serializer.GetXmlAsString(Fatura.BaseUBL); // XML byte tipinden string tipine dönüştürülür

                    var qefResult = qefFatura.FaturaGonder(strFatura, Convert.ToInt32(Fatura.BaseUBL.AdditionalDocumentReference.Where(element => element.DocumentTypeCode.Value == "CUST_INV_ID").First().ID.Value), Fatura.BaseUBL.IssueDate.Value);

                    if (qefResult.durum == 2)
                        throw new Exception("Bir Hata Oluştu!\n" + qefResult.aciklama);

                    Result.DurumAciklama = qefResult.aciklama ?? "";
                    Result.DurumKod = qefResult.gonderimDurumu.ToString();
                    Result.DurumZaman = DateTime.TryParse(qefResult.gonderimTarihi, out DateTime dz) ? dz : new DateTime(1900, 1, 1);
                    Result.EvrakNo = qefResult.belgeNo;
                    Result.UUID = qefResult.ettn;
                    Result.ZarfUUID = "";
                    Result.YanitDurum = Fatura.METHOD == "TEMELFATURA" ? 1 : 0;

                    var qefFaturaResult = qefFatura.FaturaUBLIndir(new[] { Fatura.BaseUBL.UUID.Value });
                    int EVRAKSN = Entegrasyon.GetEvraksnFromUUID(new List<string> { Result.UUID })[0];

                    Entegrasyon.UpdateEfagdn(Result, EVRAKSN, qefFaturaResult.First().Value);
                    sb.AppendLine("e-Fatura başarıyla gönderildi. \nEvrak No: " + Result.EvrakNo);
                }
                return sb.ToString();
            }

        }

        ///------------------------------------------------------------------------------------------------------------------
        ///-ESLE-------------------------------------------------------------------------------------------------------
        public override void Esle(List<int> Value, bool DontShow = false)
        {
        }
        public sealed class FIT_ING_INGBANKEFatura : EFatura
        {
            public override string Esle()
            {
                base.Esle();
                var Result = new Results.EFAGDN();
                if (Value.Count > 20)
                {
                    throw new Exception("Eşleme işlemi 20 adet üzerinde yapılamamaktadır.\nTarih aralığı verip çalıştır diyerek durum güncellemesi yapabilirsiniz.");
                }
                var fatura = new FIT.InvoiceWebService();
                fatura.WebServisAdresDegistir();

                if (UrlModel.SelectedItem == "FIT")
                {
                    var data = Entegrasyon.GetDataFromEvraksn(Value);

                    foreach (var d in data)
                    {
                        try
                        {
                            //CpmMessageBox.Show("GUID:" + d["ENTEVRAKGUID"].ToString(), "Debug");
                            var yanit = fatura.GelenUygulamaYanitByFatura(d["ENTEVRAKGUID"].ToString(), d["VERGIHESAPNO"].ToString(), d["PKETIKET"].ToString());
                            if (yanit?.Response != null)
                            {
                                if (yanit.Response.Length > 0)
                                {
                                    var ynt = yanit.Response.FirstOrDefault(elm => elm.InvoiceUUID == d["ENTEVRAKGUID"].ToString());
                                    if (ynt != null && ynt.InvResponses != null)
                                    {
                                        if (ynt.InvResponses.Length > 0)
                                        {
                                            //CpmMessageBox.Show(yanit.Response[0].InvResponses[0].ARType + "; " + yanit.Response[0].InvResponses[0].UUID + "; " + yanit.Response[0].InvoiceUUID, "Debug");

                                            Result.UUID = ynt.InvoiceUUID;
                                            Result.DurumAciklama = "";
                                            if (ynt.InvResponses[0].ARNotes != null)
                                                foreach (var note in ynt.InvResponses[0].ARNotes)
                                                    Result.DurumAciklama += note + " ";
                                            Result.DurumKod = ynt.InvResponses[0].ARType == "KABUL" ? "2" : "3";
                                            Result.DurumZaman = ynt.InvResponses[0].IssueDate;
                                            Entegrasyon.UpdateEfagdnStatus(Result);
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }
                //else if(!DontShow)
                //	CpmMessageBox.Show("Eşle butonu ile Fatura Yanıtları güncellenmemektedir.\nFatura yanıtlarını tarih aralığı vererek çalıştır butonu yardımıyla güncelleyebilirsiniz!", "Dikkat", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

                var UUIDS = Entegrasyon.GetUUIDFromEvraksn(Value).ToList();

                List<string> UUIDLower = new List<string>();
                bool OK = false;
                Exception exx = null;

                for (int i = 0; i < UUIDS.Count; i++)
                {
                    UUIDS[i] = UUIDS[i].ToUpper();
                    UUIDLower.Add(UUIDS[i].ToLower());
                }

                try
                {
                    var gonderilenler = fatura.GonderilenFaturalar(UUIDLower.ToArray());
                    Entegrasyon.UpdateEfagdn(gonderilenler);

                    OK = true;
                }
                catch (Exception ex)
                {
                    exx = ex;
                }

                try
                {
                    if (!OK)
                    {
                        var gonderilenler = fatura.GonderilenFaturalar(UUIDS.ToArray());
                        Entegrasyon.UpdateEfagdn(gonderilenler);

                        OK = true;
                    }
                }
                catch (Exception ex)
                {
                    exx = ex;
                }

                if (!OK && exx != null)
                    throw exx;

                break;
            }
        }
        public sealed class DPlanetEFatura : EFatura
        {
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
        }
        public sealed class EDMEFatura : EFatura
        {
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
        }
        public sealed class QEFEFatura : EFatura
        {
            public override string Esle()
            {
                base.Esle();
                var qefFatura = new QEF.InvoiceService();

                foreach (var d in Value)
                {
                    var qefGelen = qefFatura.GonderilenEFaturaIndir(Entegrasyon.GetUUIDFromEvraksn(Value), Value.Select(elm => elm + "").ToArray());

                    foreach (var doc in qefGelen)
                    {
                        Result.DurumAciklama = doc.Key.gonderimCevabiDetayi ?? "";
                        Result.DurumKod = doc.Key.gonderimCevabiKodu + "";
                        Result.DurumZaman = DateTime.TryParseExact(doc.Key.alimTarihi, "", CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dt) ? dt : new DateTime(1900, 1, 1);
                        Result.EvrakNo = doc.Key.belgeNo;
                        Result.UUID = doc.Key.ettn;
                        Result.ZarfUUID = "";
                        Result.YanitDurum = doc.Key.yanitDurumu == 1 ? 3 : (doc.Key.yanitDurumu == 2 ? 2 : 0);

                        Entegrasyon.UpdateEfagdn(Result, d, doc.Value, true);

                        Result.DurumAciklama = doc.Key.yanitDetayi ?? "";
                        Result.DurumKod = doc.Key.yanitDurumu == 1 ? "3" : (doc.Key.yanitDurumu == 2 ? "2" : "0");
                        Result.DurumZaman = DateTime.TryParseExact(doc.Key.yanitTarihi, "", CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dt2) ? dt2 : new DateTime(1900, 1, 1);
                        Result.UUID = doc.Key.ettn;

                        //File.WriteAllText($"C:\\QEF\\{Guid.NewGuid()}.json", JsonConvert.SerializeObject(doc.Key));
                        //File.WriteAllText($"C:\\QEF\\{Guid.NewGuid()}.json", JsonConvert.SerializeObject(Result));

                        Entegrasyon.UpdateEfagdnStatus(Result);
                        if (doc.Key.durum == 3 && doc.Key.gonderimDurumu == 4 && doc.Key.gonderimCevabiKodu == 1200)
                            Entegrasyon.UpdateEfagdnGonderimDurum(doc.Key.ettn, 3);
                        else if (doc.Key.gonderimCevabiKodu >= 1200 && doc.Key.gonderimCevabiKodu < 1300)
                            Entegrasyon.UpdateEfagdnGonderimDurum(doc.Key.ettn, 2);
                        else if (doc.Key.gonderimCevabiKodu == 1300)
                            Entegrasyon.UpdateEfagdnGonderimDurum(doc.Key.ettn, 3);
                        else if (doc.Key.gonderimDurumu > 2)
                            Entegrasyon.UpdateEfagdnGonderimDurum(doc.Key.ettn, 0);
                    }
                }
                break;
            }
        }

        ///------------------------------------------------------------------------------------------------------------------
        ///--ALINAN FATURALAR LISTESİ-----------------------------------------------------------------------------------------------
        public override List<AlinanBelge> AlinanFaturalarListesi(DateTime StartDate, DateTime EndDate)
        {

        }
        public sealed class FIT_ING_INGBANKEFatura : EFatura
        {
            public override string AlinanFaturalarListesi()
            {
                base.AlinanFaturalarListesi();
                var data = new List<AlinanBelge>();
                var fitFatura = new FIT.InvoiceWebService();

                fitFatura.WebServisAdresDegistir();

                for (DateTime dt = StartDate.Date; dt < EndDate.Date.AddDays(1); dt = dt.AddDays(1))
                {
                    Connector.m.IssueDate = dt;
                    Connector.m.EndDate = dt.AddDays(1);

                    var fitResult = fitFatura.GelenFaturalar();

                    foreach (var fatura in fitResult)
                    {
                        var data.Add(new AlinanBelge
                        {
                            EVRAKGUID = Guid.Parse(fatura.UUID),
                            EVRAKNO = fatura.ID,
                            YUKLEMEZAMAN = fatura.InsertDateTime,
                            GBETIKET = "",
                            GBUNVAN = ""
                        });
                    }
                }
                break;
                return data;

            }
        }
        public sealed class DPlanetEFatura : EFatura
        {
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
        }
        // İÇİ BOŞTU.
        public sealed class EDMEFatura : EFatura
        {
            public override string AlinanFaturalarListesi()
            {
                base.AlinanFaturalarListesi();
                var data = new List<AlinanBelge>();


                return data;

            }
        }
        public sealed class QEFEFatura : EFatura
        {
            public override string AlinanFaturalarListesi()
            {
                base.AlinanFaturalarListesi();
                var data = new List<AlinanBelge>();

                var qefFatura = new QEF.GetInvoiceService();
                qefFatura.GelenEfaturalar(StartDate, EndDate);

                foreach (var fatura in qefFatura.GelenEfaturalar(StartDate, EndDate))
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

        ///------------------------------------------------------------------------------------------------------------------
        ///-İNDİR-----------------------------------------------------------------------------------------------------------
        public override void Indir(DateTime day1, DateTime day2)
        {

        }
        public sealed class FIT_ING_INGBANKEFatura : EFatura
        {
            public override string Indir()
            {
                base.Indir();
                var fitFatura = new FIT.InvoiceWebService();
                fitFatura.WebServisAdresDegistir();

                var list = new List<string>();
                List<GetUBLListResponseType> fitGelen = new List<GetUBLListResponseType>();
                for (; day1.Date <= day2.Date; day1 = day1.AddDays(1))
                {
                    Connector.m.IssueDate = new DateTime(day1.Year, day1.Month, day1.Day, 0, 0, 0);
                    Connector.m.EndDate = new DateTime(day1.Year, day1.Month, day1.Day, 23, 59, 59);

                    var tumGelen = fitFatura.GelenFaturalar();
                    if (tumGelen.Count() > 0)
                    {
                        foreach (var fat in tumGelen)
                        {
                            list.Add(fat.UUID);
                            fitGelen.Add(fat);
                        }
                    }
                }

                List<Results.EFAGLN> fitGelenler = new List<Results.EFAGLN>();
                foreach (var f in fitGelen)
                {
                    fitGelenler.Add(new Results.EFAGLN
                    {
                        DurumAciklama = "",
                        DurumKod = "",
                        DurumZaman = f.InsertDateTime,
                        Etiket = f.Identifier,
                        EvrakNo = f.ID,
                        UUID = f.UUID,
                        VergiHesapNo = f.VKN_TCKN,
                        ZarfUUID = f.EnvUUID.ToString()
                    });
                }
                var lists = list.Split(20);

                foreach (var l in lists)
                {
                    var ubls = fitFatura.GelenFatura(l.ToArray());
                    Entegrasyon.InsertEfagln(ubls, fitGelenler.ToArray());
                }
                break;
            }
        }
        public sealed class DPlanetEFatura : EFatura
        {
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
        }
        public sealed class EDMEFatura : EFatura
        {
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
        }
        public sealed class QEFEFatura : EFatura
        {
            public override string Indir()
            {
                base.Indir();
                var qefGelenler = new List<Results.EFAGLN>();
                var qefGelenlerByte = new List<byte[]>();

                var qefFatura = new QEF.GetInvoiceService();
                foreach (var fatura in qefFatura.GelenEfaturalar(day1, day2))
                {
                    qefGelenler.Add(new Results.EFAGLN
                    {
                        DurumAciklama = fatura.Value.yanitGonderimCevabiDetayi ?? "",
                        DurumKod = fatura.Value.yanitGonderimCevabiKodu + "",
                        DurumZaman = DateTime.ParseExact(fatura.Key.belgeTarihi, "yyyyMMdd", CultureInfo.CurrentCulture),
                        Etiket = fatura.Key.gonderenEtiket,
                        EvrakNo = fatura.Value.belgeNo,
                        UUID = fatura.Value.ettn,
                        VergiHesapNo = fatura.Key.gonderenVknTckn,
                        ZarfUUID = ""
                    });
                    var ubl = ZipUtility.UncompressFile(qefFatura.GelenUBLIndir(fatura.Value.ettn));
                    qefGelenlerByte.Add(ubl);
                }
                Entegrasyon.InsertEfagln(qefGelenlerByte.ToArray(), qefGelenler.ToArray());
                break;
            }
        }

        ///------------------------------------------------------------------------------------------------------------------
        ///-KABUL-----------------------------------------------------------------------------------------------------------
        public override void Kabul(string GUID, string Aciklama)
        {

        }
        public sealed class FIT_ING_INGBANKEFatura : EFatura
        {
            public override string Kabul()
            {
                base.Kabul();
                var adp = new RemoteSqlDataAdapter("SELECT DOSYA FROM EFAGLN WITH (NOLOCK) WHERE EVRAKGUID = @GUID", appConfig.GetConnectionStrings()[0]);
                adp.SelectCommand.Parameters.AddWithValue("@GUID", GUID);

                DataTable dt = new DataTable();
                adp.Fill(ref dt);

                XmlSerializer ser = new XmlSerializer(typeof(UblInvoiceObject.InvoiceType));
                var party = ((UblInvoiceObject.InvoiceType)ser.Deserialize(new MemoryStream((byte[])dt.Rows[0][0]))).AccountingSupplierParty;

                var fitUygulamaYaniti = new FIT.InvoiceWebService();
                fitUygulamaYaniti.WebServisAdresDegistir();
                var fitResult = fitUygulamaYaniti.UygulamaYanitiGonder(GUID, true, Aciklama, party.Party);
                Entegrasyon.UpdateEfaglnStatus(new Results.EFAGLN { ZarfUUID = fitResult[0].EnvUUID }, GUID, true, Aciklama);
            }
        }
        public sealed class DPlanetEFatura : EFatura
        {
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
        }
        public sealed class EDMEFatura : EFatura
        {
            public override string Kabul()
            {
                base.Kabul();
                var edmUygulamaYaniti = new EDM.InvoiceWebService();
                edmUygulamaYaniti.WebServisAdresDegistir();
                var edmResult = edmUygulamaYaniti.KabulEFatura(GUID, Aciklama);

                Entegrasyon.UpdateEfaglnStatus(new Results.EFAGLN { ZarfUUID = "" }, GUID, true, Aciklama);
            }
        }
        public sealed class QEFEFatura : EFatura
        {
            public override string Kabul()
            {
                base.Kabul();
                var qefAdp = new RemoteSqlDataAdapter("SELECT DOSYA FROM EFAGLN WITH (NOLOCK) WHERE EVRAKGUID = @GUID", appConfig.GetConnectionStrings()[0]);
                qefAdp.SelectCommand.Parameters.AddWithValue("@GUID", GUID);

                DataTable qefDt = new DataTable();
                qefAdp.Fill(ref qefDt);

                XmlSerializer qefSer = new XmlSerializer(typeof(UblInvoiceObject.InvoiceType));
                var qefParty = ((UblInvoiceObject.InvoiceType)qefSer.Deserialize(new MemoryStream((byte[])qefDt.Rows[0][0]))).AccountingSupplierParty;

                var qefUygulamaYaniti = new QEF.GetInvoiceService();
                var qefResult = qefUygulamaYaniti.YanıtGonder(GUID, true, Aciklama, qefParty.Party);

                if (qefResult.durum == 2)
                    throw new Exception(qefResult.aciklama);

                Entegrasyon.UpdateEfaglnStatus(new Results.EFAGLN { ZarfUUID = qefResult.ettn }, GUID, true, Aciklama);
            }
        }

        ///------------------------------------------------------------------------------------------------------------------
        ///-RED--------------------------------------------------------------------------------------------------------------
        public override void Red(string GUID, string Aciklama)
        {

        }
        public sealed class FIT_ING_INGBANKEFatura : EFatura
        {
            public override string Red()
            {
                base.Red();
                var adp = new RemoteSqlDataAdapter("SELECT DOSYA FROM EFAGLN WITH (NOLOCK) WHERE EVRAKGUID = @GUID", appConfig.GetConnectionStrings()[0]);
                adp.SelectCommand.Parameters.AddWithValue("@GUID", GUID);

                DataTable dt = new DataTable();
                adp.Fill(ref dt);

                XmlSerializer ser = new XmlSerializer(typeof(UblInvoiceObject.InvoiceType));
                var party = ((UblInvoiceObject.InvoiceType)ser.Deserialize(new MemoryStream((byte[])dt.Rows[0][0]))).AccountingSupplierParty;

                var fitUygulamaYaniti = new FIT.InvoiceWebService();
                fitUygulamaYaniti.WebServisAdresDegistir();
                var fitResult = fitUygulamaYaniti.UygulamaYanitiGonder(GUID, false, Aciklama, party.Party);
                Entegrasyon.UpdateEfaglnStatus(new Results.EFAGLN { ZarfUUID = fitResult[0].EnvUUID }, GUID, false, Aciklama);
            }



        }
        public sealed class DPlanetEFatura : EFatura
        {
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
        }
        public sealed class EDMEFatura : EFatura
        {
            public override string Red()
            {
                base.Red();
                var edmUygulamaYaniti = new EDM.InvoiceWebService();
                edmUygulamaYaniti.WebServisAdresDegistir();
                var edmResult = edmUygulamaYaniti.RedEFatura(GUID, Aciklama);

                Entegrasyon.UpdateEfaglnStatus(new Results.EFAGLN { ZarfUUID = "" }, GUID, false, Aciklama);
            }
        }
        public sealed class QEFEFatura : EFatura
        {
            public override string Red()
            {
                base.Red();
                var qefAdp = new RemoteSqlDataAdapter("SELECT DOSYA FROM EFAGLN WITH (NOLOCK) WHERE EVRAKGUID = @GUID", appConfig.GetConnectionStrings()[0]);
                qefAdp.SelectCommand.Parameters.AddWithValue("@GUID", GUID);

                DataTable qefDt = new DataTable();
                qefAdp.Fill(ref qefDt);

                XmlSerializer qefSer = new XmlSerializer(typeof(UblInvoiceObject.InvoiceType));
                var qefParty = ((UblInvoiceObject.InvoiceType)qefSer.Deserialize(new MemoryStream((byte[])qefDt.Rows[0][0]))).AccountingSupplierParty;

                var qefUygulamaYaniti = new QEF.GetInvoiceService();
                var qefResult = qefUygulamaYaniti.YanıtGonder(GUID, false, Aciklama, qefParty.Party);

                if (qefResult.durum == 2)
                    throw new Exception(qefResult.aciklama);

                Entegrasyon.UpdateEfaglnStatus(new Results.EFAGLN { UUID = qefResult.ettn }, GUID, false, Aciklama);
            }
        }

        ///-----------------------------------------------------------------------------------------------------------------------
        ///-GonderilenGuncelleByDate-----------------------------------------------------------------------------------------------
        public override void GonderilenGuncelleByDate(DateTime day1, DateTime day2)
        {

        }
        public sealed class FIT_ING_INGBANKEFatura : EFatura
        {
            public override string GonderilenGuncelleByDate()
            {
                base.GonderilenGuncelleByDate();
                var Response = new Results.EFAGDN();

                var fitFatura = new FIT.InvoiceWebService();
                fitFatura.WebServisAdresDegistir();
                List<string> yanitUuid = new List<string>();
                for (; day1.Date <= day2.Date; day1 = day1.AddDays(1))
                {

                    Connector.m.IssueDate = new DateTime(day1.Year, day1.Month, day1.Day, 0, 0, 0);
                    Connector.m.EndDate = new DateTime(day1.Year, day1.Month, day1.Day, 23, 59, 59);

                    var yanit = fitFatura.GelenUygulamaYanitlari();
                    var gonderilenler = fitFatura.GonderilenFaturalar();
                    //var gonderilenDatalar = fitFatura.FaturaUBLIndir(gonderilenler.Select(elm => elm.UUID).ToArray());

                    Entegrasyon.UpdateEfagdn(gonderilenler);

                    foreach (var ynt in yanit)
                    {
                        yanitUuid.Add(ynt.UUID);
                    }
                }
                if (yanitUuid.Count > 0)
                {
                    foreach (var yanitUuidPart in yanitUuid.Split(20))
                    {
                        var yanit2 = fitFatura.GelenUygulamaYanit(yanitUuidPart.ToArray());

                        foreach (var ynt in yanit2)
                        {
                            XmlSerializer serializer = new XmlSerializer(typeof(ApplicationResponseType));
                            var fitResponse = (ApplicationResponseType)serializer.Deserialize(new MemoryStream(ynt));

                            Response.UUID = fitResponse.DocumentResponse[0].DocumentReference.ID.Value;
                            Response.DurumAciklama = "";

                            if (fitResponse.DocumentResponse[0].Response.Description != null)
                                if (fitResponse.DocumentResponse[0].Response.Description.Length > 0)
                                    Response.DurumAciklama = fitResponse.DocumentResponse[0].Response.Description[0].Value ?? "";

                            if (fitResponse.Note != null)
                                if (fitResponse.Note.Length > 0)
                                    Response.DurumAciklama += ": " + fitResponse.Note[0].Value;

                            Response.DurumKod = fitResponse.DocumentResponse[0].Response.ResponseCode.Value == "KABUL" ? "2" : "3";
                            Response.DurumZaman = fitResponse.IssueDate.Value;

                            Entegrasyon.UpdateEfagdnStatus(Response);
                        }
                    }
                }
            }

        }
        public sealed class DPlanetEFatura : EFatura
        {
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
        }
        public sealed class EDMEFatura : EFatura
        {
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
        }
        public sealed class QEFEFatura : EFatura
        {
            public override string GonderilenGuncelleByDate()
                var Response = new Results.EFAGDN();

                {
                    base.GonderilenGuncelleByDate();
            var qefFatura = new QEF.InvoiceService();
            var qefGelen = qefFatura.GonderilenFaturalar(day1, day2);

                    //var edmYanitlar = edmFatura.GelenUygulamaYanitlari(day1, day2);

                    foreach (var fatura in qefGelen)
                    {
                        if (fatura.Value.durum == 3 && fatura.Value.gonderimDurumu == 4 && fatura.Value.gonderimCevabiKodu == 1200)
                            Entegrasyon.UpdateEfagdnGonderimDurum(fatura.Value.ettn, 3);
                        else if (fatura.Value.gonderimCevabiKodu >= 1200 && fatura.Value.gonderimCevabiKodu< 1300)
                            Entegrasyon.UpdateEfagdnGonderimDurum(fatura.Value.ettn, 2);
                        else if (fatura.Value.gonderimCevabiKodu == 1300)
                            Entegrasyon.UpdateEfagdnGonderimDurum(fatura.Value.ettn, 3);
                        else if (fatura.Value.gonderimDurumu > 2)
                            Entegrasyon.UpdateEfagdnGonderimDurum(fatura.Value.ettn, 0);

                        if (fatura.Key.ettn != null)
                        {
                            Response.DurumAciklama = fatura.Value.yanitDetayi ?? "";
                            Response.DurumKod = fatura.Value.yanitDurumu == 1 ? "3" : (fatura.Value.yanitDurumu == 2 ? "2" : "0");
                            Response.DurumZaman = DateTime.TryParseExact(fatura.Value.yanitTarihi, "yyyyMMdd", CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dt) ? dt : new DateTime(1900, 1, 1);
            Response.UUID = fatura.Key.ettn;

                            Entegrasyon.UpdateEfagdnStatus(Response);
                        }
    }
}
            }
            ///-----------------------------------------------------------------------------------------------------------------------
            ///-GonderilenGuncelleByList-----------------------------------------------------------------------------------------------
            public override void GonderilenGuncelleByList(List<string> UUIDs)
{

}
public sealed class FIT_ING_INGBANKEFatura : EFatura
{
    public override string GonderilenGuncelleByList()
    {
        base.GonderilenGuncelleByList();
        Response = new Results.EFAGDN();

        var UUID20 = UUIDs.Split(20);
        foreach (var UUID in UUID20)
        {
            var fatura = new FIT.InvoiceWebService();
            fatura.WebServisAdresDegistir();

            byte[][] gonderilenler = null;
            try
            {
                for (int i = 0; i < UUID.Count; i++)
                    UUID[i] = UUID[i].ToLower();
                gonderilenler = fatura.FaturaUBLIndir(UUID.ToArray());
            }
            catch { }
            try
            {
                if (gonderilenler == null)
                {
                    for (int i = 0; i < UUID.Count; i++)
                        UUID[i] = UUID[i].ToUpper();
                    gonderilenler = fatura.FaturaUBLIndir(UUID.ToArray());
                }
            }
            catch { }

            foreach (var Gonderilen in gonderilenler)
                Entegrasyon.UpdateEfados(Gonderilen);
        }
        break;

    }
}
public sealed class DPlanetEFatura : EFatura
{
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
}
public sealed class EDMEFatura : EFatura
{
    public override string GonderilenGuncelleByList()
    {
        base.GonderilenGuncelleByList();
        Response = new Results.EFAGDN();

    }
}
public sealed class QEFEFatura : EFatura

{
    public override string GonderilenGuncelleByList()
    {
        base.GonderilenGuncelleByList();
        Response = new Results.EFAGDN();
        var qefFatura = new QEF.InvoiceService();
        var qefGelen = qefFatura.GonderilenFaturalar(UUIDs.ToArray());

        //var edmYanitlar = edmFatura.GelenUygulamaYanitlari(day1, day2);

        foreach (var fatura in qefGelen)
        {
            if (fatura.durum == 3 && fatura.gonderimDurumu == 4 && fatura.gonderimCevabiKodu == 1200)
                Entegrasyon.UpdateEfagdnGonderimDurum(fatura.ettn, 3);
            else if (fatura.gonderimCevabiKodu >= 1200 && fatura.gonderimCevabiKodu < 1300)
                Entegrasyon.UpdateEfagdnGonderimDurum(fatura.ettn, 2);
            else if (fatura.gonderimCevabiKodu == 1300)
                Entegrasyon.UpdateEfagdnGonderimDurum(fatura.ettn, 3);
            else
                Entegrasyon.UpdateEfagdnGonderimDurum(fatura.ettn, 0);

            if (fatura.ettn != null)
            {
                Response.DurumAciklama = fatura.yanitDetayi ?? "";
                Response.DurumKod = fatura.yanitDurumu == 1 ? "3" : (fatura.yanitDurumu == 2 ? "2" : "0");
                Response.DurumZaman = DateTime.TryParseExact(fatura.yanitTarihi, "", CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dt2) ? dt2 : new DateTime(1900, 1, 1);
                Response.UUID = fatura.ettn;

                Entegrasyon.UpdateEfagdnStatus(Response);
                Entegrasyon.UpdateEfados(qefFatura.FaturaUBLIndir(new[] { fatura.ettn }).First().Value);
            }
        }
        break;
    }
}

///-----------------------------------------------------------------------------------------------------------------------
///-GelenEsle-----------------------------------------------------------------------------------------------
public static void GelenEsle(List<string> uuids)
{

}

public sealed class FIT_ING_INGBANKEFatura : EFatura
{
    public override string GelenEsle()
    {
        base.GelenEsle();
        Result = new List<Results.EFAGLN>();

        var list = new List<string>();
        var fitFatura = new FIT.InvoiceWebService();

        var fitFaturalar = fitFatura.ZarfDurumSorgula2(uuids.ToArray());

        foreach (var f in fitFaturalar)
        {
            var res = new Results.EFAGLN
            {
                DurumAciklama = f.Description,
                DurumKod = f.ResponseCode,
                ZarfUUID = f.UUID
            };
            Entegrasyon.UpdateEfagln(res);
        }
    }
}
public sealed class DPlanetEFatura : EFatura
{
    public override string GelenEsle()
    {
        base.GelenEsle();
        Result = new List<Results.EFAGLN>();

    }
}
public sealed class EDMEFatura : EFatura
{
    public override string GelenEsle()
    {
        base.GelenEsle();
        Result = new List<Results.EFAGLN>();


    }
}
public sealed class QEFEFatura : EFatura

{
    public override string GelenEsle()
    {
        base.GelenEsle();
        Result = new List<Results.EFAGLN>();

    }
}
            ///-----------------------------------------------------------------------------------------------------------------------


}
