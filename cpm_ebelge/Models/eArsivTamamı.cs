namespace cpm_ebelge.Models
{
     public class eArsivProtectedValues
        {
            public dynamic doc { get; set; }
            public BaseUbl createdUBL { get; set; }
            public dynamic schematronResult {get; set; }
            public dynamic strFatura { get; set; }
            }

            public abstract class EArsiv : EArsiv_EMüstahsil
        {
            protected eArsivProtectedValues eArsivProtectedValues { get; set; } = new eArsivProtectedValues();

            public static EArsivYanit Gonder(int EVRAKSN)
            {
                EArsivYanit response = new EArsivYanit();
                var Result = new Results.EFAGDN();
                eArsivProtectedValues.doc = GeneralCreator.GetUBLArchiveData(EVRAKSN);
                if (eArsivProtectedValues.doc != null)
                {
                    Connector.m.PkEtiketi = doc.PK;
                        //doc.BaseUBL.ID.Value = doc.SendId ? doc.BaseUBL.ID.Value : "";
                        eArsivProtectedValues.createdUBL = doc.BaseUBL;  // e-Arşiv fatura UBL i oluşturulur

                    if (UrlModel.SelectedItem == "QEF")
                    {
                        if ( eArsivProtectedValues.createdUBL.Note == null)
                            eArsivProtectedValues.createdUBL.Note = new List<UblInvoiceObject.NoteType>().ToArray();

                        var noteList =  eArsivProtectedValues.createdUBL.Note.ToList();
                        noteList.Add(new UblInvoiceObject.NoteType { Value = "Gönderim Şekli: ELEKTRONIK" });

                         eArsivProtectedValues.createdUBL.Note = noteList.ToArray();
                    }

                    UBLBaseSerializer serializer = new InvoiceSerializer();  // UBL  XML e dönüştürülür
                    if (UrlModel.SelectedItem == "DPLANET")
                         eArsivProtectedValues.createdUBL.ID =  eArsivProtectedValues.createdUBL.ID.Value == $"CPM{DateTime.Now.Year}000000001" ? new UblInvoiceObject.IDType { Value = "CPM" + DateTime.Now.Year + EVRAKSN.ToString("000000000") } :  eArsivProtectedValues.createdUBL.ID;

                    // eArsivProtectedValues.createdUBL.ID =  eArsivProtectedValues.createdUBL.ID.Value == $"CPM{DateTime.Now.Year}000000001" ? new UblInvoiceObject.IDType { Value = "GIB2022000000001" } :  eArsivProtectedValues.createdUBL.ID;

                    if (Connector.m.SchematronKontrol)
                    {
                        eArsivProtectedValues.schematronResult = SchematronChecker.Check( eArsivProtectedValues.createdUBL, SchematronDocType.eArsiv);
                        if (eArsivProtectedValues.schematronResult.SchemaResult != "Başarılı" || eArsivProtectedValues.schematronResult.SchematronResult != "Başarılı")
                            throw new Exception(eArsivProtectedValues.schematronResult.Detail);
                    }
                    eArsivProtectedValues.strFatura = serializer.GetXmlAsString( eArsivProtectedValues.createdUBL); // XML byte tipinden string tipine dönüştürülür

                    Entegrasyon.EfagdnUuid(EVRAKSN, eArsivProtectedValues.doc.BaseUBL.UUID.Value);
                }
            }
            // gonder() halledildi.

            public static string TopluGonder(List<InvoiceType> Faturalar, List<string> Subeler, bool FarkliSube)
            {
                StringBuilder sb = new StringBuilder();
                var Result = new Results.EFAGDN();

                UBLBaseSerializer ser = new InvoiceSerializer();
                if (Connector.m.SchematronKontrol)
                {
                    foreach (var Fatura in Faturalar)
                    {
                        eArsivProtectedValues.schematronResult = SchematronChecker.Check(Fatura, SchematronDocType.eArsiv);
                        if (eArsivProtectedValues.schematronResult.SchemaResult != "Başarılı" || eArsivProtectedValues.schematronResult.SchematronResult != "Başarılı")
                            throw new Exception(eArsivProtectedValues.schematronResult.Detail);
                    }
                }
            }
            public sealed class FIT_ING_INGBANKEArsiv : EArsiv
            {
                
            }
            public sealed class DPlanetEArsiv : EArsiv
            {
                
            }
            public sealed class EDMEArsiv : EArsiv
            {
                
            }
            public sealed class QEFEEArsiv : EArsiv
            {
                
            }
            // Gonderde EArsivYanıt var burada beyaz geliyor  
            public override string EArsivYanit Gonder(int EVRAKSN)
            {
                EArsivYanit response = new EArsivYanit();
                var Result = new Results.EFAGDN();
                eArsivProtectedValues.doc = GeneralCreator.GetUBLArchiveData(EVRAKSN);
                if (eArsivProtectedValues.doc != null)
                {
                    Connector.m.PkEtiketi = doc.PK;
                    //doc.BaseUBL.ID.Value = doc.SendId ? doc.BaseUBL.ID.Value : "";
                    var  eArsivProtectedValues.createdUBL = doc.BaseUBL;  // e-Arşiv fatura UBL i oluşturulur

                    if (UrlModel.SelectedItem == "QEF")
                    {
                        if ( eArsivProtectedValues.createdUBL.Note == null)
                             eArsivProtectedValues.createdUBL.Note = new List<UblInvoiceObject.NoteType>().ToArray();

                        var noteList =  eArsivProtectedValues.createdUBL.Note.ToList();
                        noteList.Add(new UblInvoiceObject.NoteType { Value = "Gönderim Şekli: ELEKTRONIK" });

                         eArsivProtectedValues.createdUBL.Note = noteList.ToArray();
                    }

                    UBLBaseSerializer serializer = new InvoiceSerializer();  // UBL  XML e dönüştürülür
                    if (UrlModel.SelectedItem == "DPLANET")
                         eArsivProtectedValues.createdUBL.ID =  eArsivProtectedValues.createdUBL.ID.Value == $"CPM{DateTime.Now.Year}000000001" ? new UblInvoiceObject.IDType { Value = "CPM" + DateTime.Now.Year + EVRAKSN.ToString("000000000") } :  eArsivProtectedValues.createdUBL.ID;

                    // eArsivProtectedValues.createdUBL.ID =  eArsivProtectedValues.createdUBL.ID.Value == $"CPM{DateTime.Now.Year}000000001" ? new UblInvoiceObject.IDType { Value = "GIB2022000000001" } :  eArsivProtectedValues.createdUBL.ID;

                    if (Connector.m.SchematronKontrol)
                    {
                        eArsivProtectedValues.schematronResult = SchematronChecker.Check( eArsivProtectedValues.createdUBL, SchematronDocType.eArsiv);
                        if (eArsivProtectedValues.schematronResult.SchemaResult != "Başarılı" || eArsivProtectedValues.schematronResult.SchematronResult != "Başarılı")
                            throw new Exception(eArsivProtectedValues.schematronResult.Detail);
                    }
                    eArsivProtectedValues.strFatura = serializer.GetXmlAsString( eArsivProtectedValues.createdUBL); // XML byte tipinden string tipine dönüştürülür

                    Entegrasyon.EfagdnUuid(EVRAKSN, eArsivProtectedValues.doc.BaseUBL.UUID.Value);
                    switch (UrlModel.SelectedItem)
                    {
                        case "FIT":
                        case "ING":
                        case "INGBANK":
                            var fitEArsiv = new FIT.ArchiveWebService();
                            var fitResult = fitEArsiv.EArsivGonder(eArsivProtectedValues.strFatura,  eArsivProtectedValues.createdUBL.UUID.Value, eArsivProtectedValues.doc.SUBE);
                            byte[] ByteData = null;
                            if (fitResult.Result.Result1 == ResultType.SUCCESS)
                            {
                                Connector.m.FaturaUUID = fitResult.preCheckSuccessResults[0].UUID;
                                Connector.m.FaturaID = fitResult.preCheckSuccessResults[0].InvoiceNumber;
                                //var fitFaturaUBL = fitEArsiv.FaturaUBLIndir();
                                //ByteData = fitFaturaUBL.binaryData;

                                Result.DurumAciklama = fitResult.preCheckSuccessResults[0].SuccessDesc;
                                Result.DurumKod = fitResult.preCheckSuccessResults[0].SuccessCode + "";
                                Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
                                Result.EvrakNo = fitResult.preCheckSuccessResults[0].InvoiceNumber;
                                Result.UUID = fitResult.preCheckSuccessResults[0].UUID;
                                Result.ZarfUUID = "";
                                Result.YanitDurum = 0;

                                Entegrasyon.UpdateEfagdn(Result, EVRAKSN, null);
                                if (Connector.m.DokumanIndir)
                                {
                                    var Gonderilen = fitEArsiv.ImzaliIndir(Result.UUID, "", 0);
                                    Entegrasyon.UpdateEfados(Gonderilen.binaryData);
                                }
                            }
                            if (fitResult.Result.Result1 == ResultType.SUCCESS)
                            {
                                if (eArsivProtectedValues.doc.PRINT)
                                {
                                    response.KagitNusha = true;
                                    response.Mesaj = "e-Arşiv Fatura başarıyla gönderildi. \nEvrak No: " + fitResult.preCheckSuccessResults[0].InvoiceNumber + "\nYazdırmak İster Misiniz?";
                                    response.Dosya = ByteData;
                                }
                                else
                                {
                                    response.KagitNusha = false;
                                    response.Mesaj = "e-Arşiv Fatura başarıyla gönderildi. \nEvrak No: " + fitResult.preCheckSuccessResults[0].InvoiceNumber;
                                    response.Dosya = null;
                                }
                            }
                            else
                            {
                                throw new Exception(fitResult.preCheckErrorResults[0].ErrorDesc);
                            }
                            return response;
                        case "DPLANET":
                            var dpEArsiv = new DigitalPlanet.ArchiveWebService();
                            var dpResult = dpEArsiv.EArsivGonder(eArsivProtectedValues.strFatura,  eArsivProtectedValues.createdUBL.IssueDate.Value);

                            if (dpResult.ServiceResult == COMMON.dpInvoice.Result.Error)
                                throw new Exception(dpResult.ServiceResultDescription);

                            Connector.m.FaturaUUID = dpResult.Invoices[0].UUID;
                            Connector.m.FaturaID = dpResult.Invoices[0].InvoiceId;
                            var dpFaturaUBL = dpEArsiv.EArsivIndir(dpResult.Invoices[0].UUID);
                            ByteData = dpFaturaUBL.ReturnValue;

                            Result.DurumAciklama = dpFaturaUBL.StatusDescription;
                            Result.DurumKod = dpFaturaUBL.StatusCode + "";
                            Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
                            Result.EvrakNo = dpFaturaUBL.InvoiceId;
                            Result.UUID = dpFaturaUBL.UUID;
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
                        case "EDM":
                            var edmEArsiv = new EDM.ArchiveWebService();
                            var edmResult = edmEArsiv.EArsivGonder(eArsivProtectedValues.strFatura,  eArsivProtectedValues.createdUBL.AccountingCustomerParty.Party.PartyIdentification[0].ID.Value, Connector.m.VknTckn,  eArsivProtectedValues.createdUBL?.AccountingCustomerParty?.Party?.Contact?.ElectronicMail?.Value ?? "");

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
                        case "QEF":
                            var qefEArsiv = new QEF.ArchiveService();
                            eArsivProtectedValues.doc.SUBE = eArsivProtectedValues.doc.SUBE == "default" ? "DFLT" : eArsivProtectedValues.doc.SUBE;
                            var qefResult = qefEArsiv.EArsivGonder(eArsivProtectedValues.strFatura, Connector.m.VknTckn, EVRAKSN, eArsivProtectedValues.doc.SUBE, eArsivProtectedValues.doc.BaseUBL.IssueDate.Value.Date);

                            if (qefResult.Result.resultText != "İşlem başarılı.")
                                throw new Exception(qefResult.Result.resultText);

                            ByteData = qefResult.Belge.belgeIcerigi;

                            Result.DurumAciklama = qefResult.Result.resultText;
                            Result.DurumKod = qefResult.Result.resultCode;
                            Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
                            Result.EvrakNo = qefResult.Result.resultExtra.First(elm => elm.key.ToString() == "faturaNo").value.ToString();
                            Result.UUID = qefResult.Result.resultExtra.First(elm => elm.key.ToString() == "uuid").value.ToString();
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
                        default:
                            throw new Exception("Tanımlı Entegratör Bulunamadı!");
                    }
                }
                return response;
            }

            // Esle fonksiyonu yok. 
            public override void Esle()
            {
                throw new NotImplementedException();
            }

            public static string Iptal(int EVRAKSN, string EVRAKNO, string GUID, decimal TUTAR)
            {
            }
            public sealed class FIT_ING_INGBANKEArsiv : EArsiv
            {
                
            }
            public sealed class DPlanetEArsiv : EArsiv
            {
                
            }
            public sealed class EDMEArsiv : EArsiv
            {
                
            }
            public sealed class QEFEEArsiv : EArsiv
            {
                
            }
            public static string Itiraz(string EVRAKNO, UBL.ObjectionClass Objection)
            {
            }
            public sealed class FIT_ING_INGBANKEArsiv : EArsiv
            {
                
            }
            public sealed class DPlanetEArsiv : EArsiv
            {
                
            }
            public sealed class EDMEArsiv : EArsiv
            {
                
            }
            public sealed class QEFEEArsiv : EArsiv
            {
                
            }

            ///Sadece EArsivde olanlar

            public static void GonderilenGuncelle(int EVRAKSN)

            {
                var Result = new Results.EFAGDN();
                switch (UrlModel.SelectedItem)
                {
                    case "FIT":
                    case "ING":
                    case "INGBANK":
                        var fitArsiv = new FIT.ArchiveWebService();
                        fitArsiv.WebServisAdresDegistir();

                        string UUID = Entegrasyon.GetUUIDFromEvraksn(new List<int> { EVRAKSN })[0];
                        var evrakno = Entegrasyon.GetEvraknoFromEvraksn(new[] { EVRAKSN }.ToList())[0];

                        getSignedInvoiceResponseType ars = null;
                        Exception exx = null;

                        try
                        {
                            ars = fitArsiv.ImzaliIndir(UUID.ToUpper(), evrakno, EVRAKSN);
                        }
                        catch (Exception ex)
                        {
                            exx = ex;
                        }
                        try
                        {
                            ars = fitArsiv.ImzaliIndir(UUID.ToLower(), evrakno, EVRAKSN);
                        }
                        catch (Exception ex)
                        {
                            exx = ex;
                        }

                        if (ars != null)
                        {
                            Result.DurumAciklama = ars.Detail;
                            Result.DurumKod = "150";
                            Result.DurumZaman = DateTime.Now;
                            Result.EvrakNo = ars.invoiceNumber;
                            Result.UUID = ars.UUID;
                            Result.ZarfUUID = "";
                            Result.YanitDurum = 0;

                            Entegrasyon.UpdateEfagdn(Result, EVRAKSN, ars.binaryData, onlyUpdate: true);
                        }
                        else if (exx != null)
                        {
                            throw exx;
                            //Değiştir--CpmMessageBox.Show($"Entegratörden Fatura Bilgisi Dönmedi.\nEvraksn:{EVRAKSN}\nEvrakno:{evrakno}\nUUID:{UUID}", "Dikkat", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                            //Değiştir--CpmMessageBox.Show($"Entegratör Açıklaması: {exx.Message}", "Dikkat", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                        }

                        break;
                    case "DPLANET":
                        var dpArsiv = new DigitalPlanet.ArchiveWebService();
                        dpArsiv.WebServisAdresDegistir();

                        var uuid = Entegrasyon.GetUUIDFromEvraksn(new[] { EVRAKSN }.ToList());

                        if (uuid[0] != "")
                        {
                            var dpArs = dpArsiv.EArsivIndir(uuid[0]);
                            XmlSerializer ser = new XmlSerializer(typeof(InvoiceType));

                            var byteData = dpArsiv.EArsivIndir(dpArs.UUID).ReturnValue;
                            //File.WriteAllBytes("test.xml", byteData);
                            var i = (InvoiceType)ser.Deserialize(new MemoryStream(byteData));
                            if (i.AdditionalDocumentReference.First(elm => elm.DocumentTypeCode.Value == "CUST_INV_ID").ID.Value == EVRAKSN + "")
                            {
                                Result.DurumAciklama = dpArs.StatusDescription;
                                Result.DurumKod = dpArs.StatusCode + "";
                                Result.DurumZaman = DateTime.Now;
                                Result.EvrakNo = dpArs.InvoiceId;
                                Result.UUID = dpArs.UUID;
                                Result.ZarfUUID = "";
                                Result.YanitDurum = 0;

                                Entegrasyon.UpdateEfagdn(Result, EVRAKSN, byteData, true);
                            }
                        }
                        else
                        {
                            var dt = Entegrasyon.GetGonderimZaman(new[] { EVRAKSN }.ToList())[0];

                            if (dt == new DateTime(1900, 1, 1))
                                dt = DateTime.Now;

                            var start = new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0);
                            var end = new DateTime(dt.Year, dt.Month, dt.Day, 23, 59, 59);

                            var invoices = dpArsiv.EArsivIndir(start, end);

                            XmlSerializer ser = new XmlSerializer(typeof(InvoiceType));

                            foreach (var inv in invoices.Invoices)
                            {
                                var byteData = dpArsiv.EArsivIndir(inv.UUID).ReturnValue;
                                //File.WriteAllBytes("test.xml", byteData);
                                var i = (InvoiceType)ser.Deserialize(new MemoryStream(byteData));
                                if (i.AdditionalDocumentReference.First(elm => elm.DocumentTypeCode.Value == "CUST_INV_ID").ID.Value == EVRAKSN + "")
                                {
                                    Result.DurumAciklama = inv.StatusDescription;
                                    Result.DurumKod = inv.StatusCode + "";
                                    Result.DurumZaman = DateTime.Now;
                                    Result.EvrakNo = inv.InvoiceId;
                                    Result.UUID = inv.UUID;
                                    Result.ZarfUUID = "";
                                    Result.YanitDurum = 0;

                                    Entegrasyon.UpdateEfagdn(Result, EVRAKSN, byteData, true);
                                }
                            }
                        }
                        break;
                    case "EDM":
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
                    case "QEF":
                        var qefArsiv = new QEF.ArchiveService();
                        var qefGelen = qefArsiv.EArsivIndir(Entegrasyon.GetUUIDFromEvraksn(new List<int> { EVRAKSN })[0]);

                        Result.DurumAciklama = qefGelen.Result.resultText;
                        Result.DurumKod = qefGelen.Result.resultCode;
                        Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
                        Result.EvrakNo = qefGelen.Result.resultExtra.First(elm => elm.key.ToString() == "faturaNo").value.ToString();
                        Result.UUID = qefGelen.Result.resultExtra.First(elm => elm.key.ToString() == "uuid").value.ToString();
                        Result.ZarfUUID = "";
                        Result.YanitDurum = 0;

                        Entegrasyon.UpdateEfagdn(Result, EVRAKSN, qefGelen.Belge.belgeIcerigi, onlyUpdate: true);

                        //if (qefGelen[0].HEADER.EARCHIVE_REPORT_UUID != null)
                        //    Entegrasyon.UpdateEfagdnGonderimDurum(qefGelen[0].UUID, 4);
                        //else if (qefGelen[0].HEADER.STATUS_DESCRIPTION == "SUCCEED")
                        //    Entegrasyon.UpdateEfagdnGonderimDurum(qefGelen[0].UUID, 3);
                        //else
                        //    throw new Exception(EVRAKSN + " Seri Numaralı Evrak Gönderilenler Listesinde Bulunmamaktadır!");
                        break;
                    default:
                        throw new Exception("Tanımlı Entegratör Bulunamadı!");
                }
            }
        }
}
