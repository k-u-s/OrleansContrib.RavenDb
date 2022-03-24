using System.Collections.Generic;

namespace OrleansContrib.Reminders.RavenDb.Options;

public class DatabaseSettings
{
    public string DatabaseName { get; set; }
    public List<string> ServerUrls { get; set; } = new List<string>();
    public string CertificatePath { get; set; }
    public string CertificatePassword { get; set; }
}