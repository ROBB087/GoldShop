using GoldShopCore.Models;
using GoldShopCore.Services;

const string privateKeyXml = "<RSAKeyValue><Modulus>xtOrITHZZh0mXFtzge9oM2LjDKoJ7vThphUt2MqODyFPhiCb5P4hmpFX3Fux7QQpqupTF2ffVJXJiq3vh51UT6fYwaocvt9qtFmFWRskjgJNhEJfcE8ymEBSQehgT9QgRdRxgmQUcI2470liPSHvjP/pFgl6WYVFDlumEVcIxrlWTy+DHNkKcyUFDrO6PnCtBGGNxqyJ+tK1kv8WeDkIcG4BF5pC97xBzigOtozxwdH7niIwtU8yNcelNm17P8drHwbjkxrvhT1LPl9NwSGZgajWGvAufmn1SK1nVDCwXcT8NDsYz8vB3rpnifPet9Sa2pQU2e7z4sSJlW4yM1xl2Q==</Modulus><Exponent>AQAB</Exponent><P>1PWAs5H0K2gSGE2Si/idtWwT5+TmBV3feOI/F220LutRsypOv0LC2GRBjoGCeu+EQw4Hm1NLiSfMGPFNfJ63i5o7XK7KxL6Lf/wNkl3mCUfS7d5cQ8k7QcX1DM9UaN7jJ69yj/43NbNWMphPYXGMrcM5fgD9U2QB1bW+062q7b8=</P><Q>7wL3s+wIGAVULLmMCP7P+kIKneYv3fW/ACnONilRJGk4sJHav7Q6vE5jqSVC2GBP6uzIVkiLIjVhYVRUOfCMO6CqHTMoNEvCWlszsAvxl7SxE/XqPtK/ti9wMCFu+Mc1s9EnBFxEke3GONvupri1bmOEORcOQ0N2L9sQreeUwmc=</Q><DP>dgRDucAV34RCGuPKZfV2eNcXRPjOHIVEVfqT2kj6hNH2KeM1VrsJvd/5kJ7nD3fzBTIeNw73GmBKtDvtpDVVJHFpmlhnmJa8OkYVw1p0JAqAsz/6Q7qeMzogLmQrtB5pJlINnAzWzdS/3TQZMbg6rQU2tESaHv+aILQit65TvoM=</DP><DQ>j3fdjh+xYBHazBn4h/HEj7kSvGNO+lIn+4YcpQA6F7wdbkeu4gHie+QmCIM4U7/EWQUW4EwdUERwlsbS5BCTbLttQafSi0mqeShjp1oUA/dPj+a+XEWPFGH3WrzG6whRIQX8AK8N7fanVLwXzfXz2jZcSRSg2BlnmSRLJ8hp0CM=</DQ><InverseQ>GI0ydRNaWE2TcuaSiMF/n3s9P2DjfTV+OsYcbp8KHuDxycSBIHZJIlwgkYq3WfRw42PL8oFE+BlJgV/lRX3GnFtH6VVD0kYOp6oTFyn224KXNCl6Ly7VoxHtNlwaKLYbyh9llScR7VKFUcv015KlhhUM/0z1HtOJ88xrLhzk34k=</InverseQ><D>OaFY1QJR9Vs1p0eKr3rpRRvAnAcdYfnw/ebdpxzvGEubdVE2XqWar+a5BNI/PGgce8H326zr+uR/yoaoCzL7ISuRlHDubBTuJOBd2noXmmHofhGPAEq0a5UZQqlMYVcnE5aEYDsKAaXSmOk4ZGfVu5ThxtVwdUo/ve2qsRP9IzxKWoAwrVdpCK1Fk+0TJQYk0EAVrEL6gzSM5sBmbYBDWpYbIkoJRf3+xx6hn51Iu9nJJTHNwGBDFb15/NnTZSaC9rkInaTruK52l0cKGn8CXfVS4jiD7dol62Hbh35DdtGZmPeUITzRV9LXIqPWUKrJtu7Htk9k0q9XBM/KecnwkQ==</D></RSAKeyValue>";

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    Console.WriteLine("Usage: GoldShopLicenseTool <machine-id> [licensed-to]");
    Console.WriteLine("Example: GoldShopLicenseTool ABCDE-FGHIJ-KLMNO-PQRST-UVWXY \"Abo Emad\"");
    return;
}

var machineId = args[0].Trim();
var licensedTo = args.Length > 1
    ? string.Join(' ', args.Skip(1)).Trim()
    : "Abo Emad";

if (string.IsNullOrWhiteSpace(machineId))
{
    Console.WriteLine("Machine ID is required.");
    return;
}

var license = new AppLicense
{
    LicensedTo = licensedTo,
    MachineId = machineId,
    ProductCode = "GoldShopWpf",
    IssuedAtUtc = DateTime.UtcNow
};

var licenseKey = LicenseKeyCodec.CreateKey(license, privateKeyXml);
Console.WriteLine("Licensed to: " + licensedTo);
Console.WriteLine("Machine ID : " + machineId);
Console.WriteLine();
Console.WriteLine("License key:");
Console.WriteLine(licenseKey);
