using System.Xml.Serialization;

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
                    if (eArsivProtectedValues.createdUBL.Note == null)
                        eArsivProtectedValues.createdUBL.Note = new List<UblInvoiceObject.NoteType>().ToArray();

                    var noteList = eArsivProtectedValues.createdUBL.Note.ToList();
                    noteList.Add(new UblInvoiceObject.NoteType { Value = "Gönderim Şekli: ELEKTRONIK" });

                    eArsivProtectedValues.createdUBL.Note = noteList.ToArray();
                }

                UBLBaseSerializer serializer = new InvoiceSerializer();  // UBL  XML e dönüştürülür
                if (UrlModel.SelectedItem == "DPLANET")
                    eArsivProtectedValues.createdUBL.ID = eArsivProtectedValues.createdUBL.ID.Value == $"CPM{DateTime.Now.Year}000000001" ? new UblInvoiceObject.IDType { Value = "CPM" + DateTime.Now.Year + EVRAKSN.ToString("000000000") } : eArsivProtectedValues.createdUBL.ID;

                // eArsivProtectedValues.createdUBL.ID =  eArsivProtectedValues.createdUBL.ID.Value == $"CPM{DateTime.Now.Year}000000001" ? new UblInvoiceObject.IDType { Value = "GIB2022000000001" } :  eArsivProtectedValues.createdUBL.ID;

                if (Connector.m.SchematronKontrol)
                {
                    eArsivProtectedValues.schematronResult = SchematronChecker.Check(eArsivProtectedValues.createdUBL, SchematronDocType.eArsiv);
                    if (eArsivProtectedValues.schematronResult.SchemaResult != "Başarılı" || eArsivProtectedValues.schematronResult.SchematronResult != "Başarılı")
                        throw new Exception(eArsivProtectedValues.schematronResult.Detail);
                }
                eArsivProtectedValues.strFatura = serializer.GetXmlAsString(eArsivProtectedValues.createdUBL); // XML byte tipinden string tipine dönüştürülür

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
                var eArsivProtectedValues.createdUBL = doc.BaseUBL;  // e-Arşiv fatura UBL i oluşturulur

                if (UrlModel.SelectedItem == "QEF")
                {
                    if (eArsivProtectedValues.createdUBL.Note == null)
                        eArsivProtectedValues.createdUBL.Note = new List<UblInvoiceObject.NoteType>().ToArray();

                    var noteList = eArsivProtectedValues.createdUBL.Note.ToList();
                    noteList.Add(new UblInvoiceObject.NoteType { Value = "Gönderim Şekli: ELEKTRONIK" });

                    eArsivProtectedValues.createdUBL.Note = noteList.ToArray();
                }

                UBLBaseSerializer serializer = new InvoiceSerializer();  // UBL  XML e dönüştürülür
                if (UrlModel.SelectedItem == "DPLANET")
                    eArsivProtectedValues.createdUBL.ID = eArsivProtectedValues.createdUBL.ID.Value == $"CPM{DateTime.Now.Year}000000001" ? new UblInvoiceObject.IDType { Value = "CPM" + DateTime.Now.Year + EVRAKSN.ToString("000000000") } : eArsivProtectedValues.createdUBL.ID;

                // eArsivProtectedValues.createdUBL.ID =  eArsivProtectedValues.createdUBL.ID.Value == $"CPM{DateTime.Now.Year}000000001" ? new UblInvoiceObject.IDType { Value = "GIB2022000000001" } :  eArsivProtectedValues.createdUBL.ID;

                if (Connector.m.SchematronKontrol)
                {
                    eArsivProtectedValues.schematronResult = SchematronChecker.Check(eArsivProtectedValues.createdUBL, SchematronDocType.eArsiv);
                    if (eArsivProtectedValues.schematronResult.SchemaResult != "Başarılı" || eArsivProtectedValues.schematronResult.SchematronResult != "Başarılı")
                        throw new Exception(eArsivProtectedValues.schematronResult.Detail);
                }
                eArsivProtectedValues.strFatura = serializer.GetXmlAsString(eArsivProtectedValues.createdUBL); // XML byte tipinden string tipine dönüştürülür

                Entegrasyon.EfagdnUuid(EVRAKSN, eArsivProtectedValues.doc.BaseUBL.UUID.Value);
                switch (UrlModel.SelectedItem)
                {
                    case "FIT":
                    case "ING":
                    case "INGBANK":
                        var fitEArsiv = new FIT.ArchiveWebService();
                        var fitResult = fitEArsiv.EArsivGonder(eArsivProtectedValues.strFatura, eArsivProtectedValues.createdUBL.UUID.Value, eArsivProtectedValues.doc.SUBE);
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
                        var dpResult = dpEArsiv.EArsivGonder(eArsivProtectedValues.strFatura, eArsivProtectedValues.createdUBL.IssueDate.Value);

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

        }


    }