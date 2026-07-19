using WhatsApp.Core.AspNetCore.DependencyInjection;
using WhatsApp.Core.AspNetCore.Webhooks;
using WhatsApp.Core.DependencyInjection;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args, EnvironmentName = "Development" });
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Services.AddWhatsAppCore(o => {
    o.PhoneNumberId = "1";
    o.AccessToken = "t";
    o.GraphApiVersion = "v21.0";
    o.AppSecret = "s";
    o.VerifyToken = "v";
});
builder.Services.AddWhatsAppCore("support", o => {
    o.PhoneNumberId = "2";
    o.AccessToken = "t2";
    o.GraphApiVersion = "v21.0";
    o.AppSecret = "s2";
    o.VerifyToken = "v2";
});
builder.Services.AddWhatsAppWebhooks();
var app = builder.Build();
app.MapWhatsAppWebhook("/webhooks/support", o => o.AccountName = "support");
app.MapWhatsAppWebhook("/webhooks/sales", o => o.AccountName = "Default");
try {
    await app.StartAsync();
    Console.WriteLine("STARTED_OK");
    await app.StopAsync();
} catch (Exception ex) {
    Console.WriteLine("FAIL: " + ex.GetType().Name + ": " + ex.Message);
    for (var i = ex; i != null; i = i.InnerException) Console.WriteLine("  -> " + i.GetType().Name + ": " + i.Message);
}
