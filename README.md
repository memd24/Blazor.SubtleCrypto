# Blazor.SubtleCrypto
Provides services for encrypt and decrypt data. The data is protected using SubtleCrypto encrypt/decrypt methods and AES-GCM algorithm and returned in ciphertext.
Because it uses JSInterop, this library can run on Blazor WebAssembly (client-side project) as well as Blazor Server.
<br /><br />
# Install
```
Install-Package Blazor.SubtleCrypto
```  
To know the latest version: https://www.nuget.org/packages/Blazor.SubtleCrypto
<br /><br />
# How to use in Blazor WebAssembly & Blazor Server

Add to Program.cs
``` csharp
using Blazor.SubtleCrypto;

builder.Services.AddSubtleCrypto(opt => 
    opt.Key = "ELE9xOyAyJHCsIPLMbbZHQ7pVy7WUlvZ60y5WkKDGMSw5xh5IM54kUPlycKmHF9VGtYUilglL8iePLwr" //Use another key
);
```

Add this in your specific page .razor. If you need to encrypt/decrypt in diferent pages or components, add it in _Imports.razor
``` csharp
@using Blazor.SubtleCrypto
@inject ICryptoService Crypto
```

### Encrypt/Decrypt
Converts textplain to a ciphertext and vice versa
``` csharp
CryptoResult encrypted = await Crypto.EncryptAsync("The krabby patty secret formula is...");
//encrypted.Value returns: emw4a0xHNDQ3WnppU9jxTBZ4SseC1/foz4aKC+oA3nZZHvNIwVXwIy0kpm68N/SgzjpL6dlihEz8q8opjbc9OcE=

string decrypted = await Crypto.DecryptAsync(encrypted.Value);
//decrypted returns: "The krabby patty secret formula is..."
```
Converts object class to a ciphertext and vice versa
``` csharp
CryptoResult encrypted = await Crypto.EncryptAsync(new myModel{name="Bob", age=36});
//encrypted.Value returns: MFA5QlJsWDNlZXY0cEE8EI5O74iI/4pMr5k3gjKqWSnVFU6nEy7ypBreOW7jg9Sd+VPHmnMcgs5pivb9FI4/CVfoX...

string decrypted = await Crypto.DecryptAsync(encrypted.Value);
//decrypted returns: '{"name":"Bob","age":36}'

myModel decrypted = await Crypto.DecryptAsync<myModel>(encrypted.Value);
//decrypted returns a populated myModel object
```
Converts a list of objects  to a ciphertext and vice versa
``` csharp
CryptoResult encrypted = await Crypto.EncryptAsync(myListModel);
//encrypted.Value returns: OHRCMUJ2MDhLc2JTDerrg1DxyO0/Src7dyjRc+a4ulWYspoC519b1WcRuGo3u3+JuG+Lp8MLTnsXjXXklxQ9zQ7jeN...

string decrypted = await Crypto.DecryptAsync(encrypted.Value);
//decrypted returns: '[{"name":"Bob","age":36},{"name":"Pat","age":38}]'

myModel decrypted = await Crypto.DecryptAsync<List<myModel>>(encrypted.Value);
//decrypted returns a populated List<myModel> object
```
Converts each string of a list into an individual ciphertext and vice versa
``` csharp
//To encrypt a list we use EncryptListAsync()
List<string> myList = new List<string> { "Bob", "Pat", "Don" };
List<CryptoResult> encrypted = await Crypto.EncryptListAsync(myList);

//To decrypt a list we use DecryptListAsync()
List<string> decrypted = await Crypto.DecryptListAsync(encrypted.Select(x=> x.Value).ToList());

//You can use DecryptListAsync<T>() to get a list of object
List<MyModel> decrypted = await Crypto.DecryptListAsync<MyModel>(encrypted.Select(x=> x.Value).ToList());
```

<br />

CryptoResult info:
| Property | Description |
| --- | --- |
| Origin  | Original data converted in string|
| Secret  | If there is no key in Program.cs, IV and Key will be generated automatically, otherwise they will be null.  |
| Status  | Returns true if encryption/decryption was successfull  |
| Value   | Returns ciphertext  |

<br />


### Encrypt/Decrypt with dynamic key
You can skip setting the key to Program.cs and each encryption will generate a different key and IV.<br />
In Program.cs
``` csharp
using Blazor.SubtleCrypto;

builder.Services.AddSubtleCrypto();
```
Example using a single string
``` csharp
CryptoResult encrypted = await Crypto.EncryptAsync("The krabby patty secret formula is...");
//encrypted.Value returns: WEs5SHJCMWN6ZlZsjN7KgUEJB6cu5eLy4tzsPZ2IHuegOW9DdEik+B+nY/aUnHORacAVmaX6F/xKQgLs0p20sHY=
//encrypted.Secret.Key returns: RRR0dYZ6eCHX91nzkNx9SqMPOt3rBxZqV5zGPAN66p5EaYgATzW3j7FoDIC3Hu04LdKT4cKnpMBUdEhLVoj...

//To decrypt we need to ue CryptoInput model and set the ciphertext(Value) and the key
string decrypted = await Crypto.DecryptAsync(new CryptoInput { Value = encrypted.Value, Key = encrypted.Secret.Key});
//decrypted returns: "The krabby patty secret formula is..."
```

Example using a list of object
``` csharp
List<CryptoResult> encryptedList = await Crypto.EncryptListAsync(list);
//returns a list of CryptoResult each one with distinct Key & IV

//To decrypt we need to ue CryptoInput model and set the ciphertext(Value) and the key
List<CryptoInput> cryptoInputs = encryptedList.Select(x => new CryptoInput { Key = x.Secret.Key, Value = x.Value }).ToList();
List<MyModel> decrypted = await Crypto.DecryptListAsync<MyModel>(cryptoInputs);
//decrypted returns a populated List<myModel> object
```
<br /><br />
# Additional Info
Because in Blazor WebAssembly everything runs on a single thread, it is recommended to avoid using it with long data or you will notice a freeze in the ui.<br /><br />
Unlike blazor webassembly blazor server does not have this problem but for long data it is recommended to increase the buffer size.<br /><br />
In Program.cs
``` csharp
builder.Services.AddServerSideBlazor().AddHubOptions(hub => hub.MaximumReceiveMessageSize = 100 * 1024 * 1024); //example 100MB
```
<br /><br />
Blazor.SubtleCrypto uses Sublte Crypto functions through JSInterop, for this the client-side methods were injected through eval (I know) and because of this, 
to increase a little more security on this side, 
the methods they were obfuscated and once injected they are called like any other js function through InvokeAsync and get the ciphertext
<br /><br />
Example of js script using subtle.encrypt()
``` js
 function encryptMessage(plaintext) {
  let enc = new TextEncoder();
  let utf = enc.encode(plaintext);
  let iv = window.crypto.getRandomValues(new Uint8Array(12));
  let ciphertext = window.crypto.subtle.encrypt(
    {
      name: "AES-GCM",
      iv: iv
    },
    key,
    enc
  );
  console.log(ciphertext);
}

```
Example same script but obfuscated:
``` 
function _0x133b(_0x5236fc,_0x149baf){const _0x1eeca7=_0x363c();return _0x133b=function(_0x1b94c0,_0x496114){_0x1b94c0=_0x1b94c0-(-0x1f7*-0x4+0x1*0x368+0x6*-0x1b1);let _0x5c8a5b=_0x1eeca7[_0x1b94c0];return _0x5c8a5b;},_0x133b(_0x5236fc,_0x149baf);}(function(_0x3271a2,_0x38de6b){function _0x34f434(_0x26a5b9,_0x1bd333,_0x5ebd5f,_0x19546a){return _0x133b(_0x1bd333-0x37d,_0x5ebd5f);}function _0x18e45c(_0xc2306d,_0x2970d9,_0xfc0cbd,_0x21a00e){return _0x133b(_0x2970d9-0xad,_0x21a00e);}const _0xc20dc9=_0x3271a2();while(!![]){try{const _0x1060b3=parseInt(_0x34f434(0x49e,0x49c,0x49a,0x493))/(0xccc+0x194a+0x2615*-0x1)+parseInt(_0x34f434(0x4a5,0x4ab,0x4ad,0x4ae))/(0x252e+0x1760+-0x3c8c)+-parseInt(_0x34f434(0x49d,0x4a0,0x49b,0x4a7))/(0x2*0x427+0x10*0x165+-0x5*0x61f)*(parseInt(_0x18e45c(0x1cf,0x1cf,0x1d1,0x1d0))/(0x17ef+-0x914+0x83*-0x1d))+parseInt(_0x18e45c(0x1cf,0x1cb,0x1d2,0x1d2))/(-0x12d+-0x1b23*0x1+0x1c55)*(parseInt(_0x34f434(0x4b2,0x4aa,0x4aa,0x4a6))/(-0x7bb*-0x3+0x1816+-0x2f41))+-parseInt(_0x34f434(0x4a1,0x4a8,0x4a5,0x4ab))/(-0xb61*-0x1+0x1db4+-0x2*0x1487)+parseInt(_0x18e45c(0x1d4,0x1cd,0x1cc,0x1cb))/(0x2054+-0xe44+-0x1208)+-parseInt(_0x18e45c(0x1d2,0x1d2,0x1d3,0x1d4))/(0x13dd+0x1de*-0x13+-0x7d3*-0x2)*(parseInt(_0x34f434(0x4a3,0x4a4,0x4a3,0x49b))/(0x10e5+-0x647*-0x2+-0x1*0x1d69));if(_0x1060b3===_0x38de6b)break;else _0xc20dc9['push'](_0xc20dc9['shift']());}catch(_0x968782){_0xc20dc9['push'](_0xc20dc9['shift']());}}}(_0x363c,0x60054+0x40ea6+-0x6ee71));function encryptMessage(_0x2bd016){const _0x38ab2a={};_0x38ab2a['tLnpW']=_0x4f954a(-0x50,-0x4d,-0x46,-0x4d);const _0x3bfd4c=_0x38ab2a;function _0x4f954a(_0x3bb992,_0x56d1fc,_0x2ad666,_0x1d2fcc){return _0x133b(_0x1d2fcc- -0x16e,_0x56d1fc);}let _0x2d65b4=new TextEncoder(),_0x4750a4=_0x2d65b4[_0x5cb37(-0x64,-0x64,-0x66,-0x68)](_0x2bd016),_0x3711e7=window['crypto'][_0x5cb37(-0x67,-0x65,-0x62,-0x66)+'alues'](new Uint8Array(-0xc1f*-0x1+0x1*0x1dee+-0x2a01));const _0x4ad463={};function _0x5cb37(_0x55c386,_0x1f16f8,_0x407808,_0x38a357){return _0x133b(_0x407808- -0x18e,_0x38a357);}_0x4ad463[_0x4f954a(-0x46,-0x47,-0x45,-0x3f)]=_0x3bfd4c[_0x4f954a(-0x45,-0x4f,-0x46,-0x48)],_0x4ad463['iv']=_0x3711e7;let _0xfee287=window['crypto'][_0x4f954a(-0x3f,-0x3c,-0x3f,-0x44)][_0x4f954a(-0x4e,-0x4c,-0x45,-0x45)](_0x4ad463,key,_0x2d65b4);console[_0x5cb37(-0x69,-0x62,-0x6a,-0x71)](_0xfee287);}function _0x363c(){const _0x4c8334=['log','2187693pncwgn','tLnpW','10KtFBWK','encode','encrypt','subtle','2345994mqjKPo','getRandomV','2190xBHEbw','719272Rykswt','name','1060PuYewl','147620RaNoRX','2444720tBXyyQ','AES-GCM','71380Rhorkx','18qhSaze'];_0x363c=function(){return _0x4c8334;};return _0x363c();}
```

Feel free to use this repo.

