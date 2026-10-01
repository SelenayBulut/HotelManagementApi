using System.Text.Json.Serialization;

namespace HotelManagementApi.Models
{

    // Sistemde kullanılan sabit seçenekleri ve durumları enum olarak tanımlar.
    [JsonConverter(typeof(JsonStringEnumConverter))]

    public enum UserRole //kullanıcı rolleri
    {
        Admin, //sistem yoneticisi
        HotelOwner, //otelsahibi
        Customer //musteri
    }
    public enum ReservationStatus //rezervasyon durumu 
    {
        Confirmed, //onaylanmış aktif
        Cancelled, //iptal
        Completed//konaklama tamamlanmış
    }
    public enum RoomType //oda türleri
    {
        Standard, //standart oda
        Deluxe, // üst seviye
        Suite //suit oda
    }

    public enum TransactionType //ödeme kaydı türü
    {
        Payment, //normal ödeme
        Refund, //iade işlmei
        Penalty //ceza işlemi
    }

    public enum PaymentStatus //işlemin durumu
    {
        Success, //başarılı   
        Failed, //başarısız
        Pending,//beklemede
        Refunded //iade edildi
    }
}
