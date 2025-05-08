# Blazor.SubtleCrypto
Provides services for encrypt and decrypt data. The data is protected using SubtleCrypto encrypt/decrypt methods and AES-GCM algorithm and returned in ciphertext.
Because it uses JSInterop, this library can run on Blazor WebAssembly (client-side project) as well as Blazor Server.

# Install
```
Install-Package Blazor.SubtleCrypto
```  
To know the latest version: https://www.nuget.org/packages/Blazor.SubtleCrypto

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
//encrypted.Value returns: emw4a0xHNDQ3WnppU9jxTBZ4SseC1/fkpm68N/SgzjpL6dlihEz8q8opjbc9OcE=

string decrypted = await Crypto.DecryptAsync(encrypted.Value);
//decrypted returns: "The krabby patty secret formula is..."
```
Converts object class to a ciphertext and vice versa
``` csharp
CryptoResult encrypted = await Crypto.EncryptAsync(new myModel{name="Bob", age=36});
//encrypted.Value returns: MFA5QlJsWDNlZXY0cEE8EI5O74iI/4pMr59Sd+VPHmnMcgs5pivb9FI4/CVfoX...

string decrypted = await Crypto.DecryptAsync(encrypted.Value);
//decrypted returns: '{"name":"Bob","age":36}'

myModel decrypted = await Crypto.DecryptAsync<myModel>(encrypted.Value);
//decrypted returns a populated myModel object
```
Converts a list of objects  to a ciphertext and vice versa
``` csharp
CryptoResult encrypted = await Crypto.EncryptAsync(myListModel);
//encrypted.Value returns: OHRCMUJ2MDhLc2JTDerrg1DxyO0/Srcu3+JuG+Lp8MLTnsXjXXklxQ9zQ7jeN...

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



CryptoResult info:
| Property | Description |
| --- | --- |
| Origin  | Original data converted in string|
| Secret  | If there is no key in Program.cs, IV and Key will be generated automatically, otherwise they will be null.  |
| Status  | Returns true if encryption/decryption was successfull  |
| Value   | Returns ciphertext  |




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
//encrypted.Value returns: WEs5SHJCMWN6ZlZsjN7KgUEJB6cu5eLy4tzORacAVmaX6F/xKQgLs0p20sHY=
//encrypted.Secret.Key returns: RRR0dYZ6eCHX91nzkNx9oTzW3j7FoDIC3Hu04LdKT4cKnpMBUdEhLVoj...

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



### Note
For Blazor WASM Net8 and later, it may require to modify rendermode or  execute ICryptoService injection after render.<br />
Example
``` csharp
@code {
    private ICryptoService? Crypto;
    private async Task TestSubtleCrypto(){
    Crypto = ServiceProvider.GetService<ICryptoService>();
    if (Crypto is not null)
        {
            //do it..
        }
    }
}
```
