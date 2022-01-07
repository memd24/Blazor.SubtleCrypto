using Microsoft.JSInterop;
using System.Text;
using System.Text.Json;

namespace Blazor.SubtleCrypto
{
    public class CryptoService : ICryptoService
    {
        private readonly string _Encryption;
        private readonly string _Algorithm;
        private string _Key;
        private bool _isGlobalKey;
        private readonly bool _DoubleEncryption;
        private readonly IJSRuntime _jsRuntime;
        private readonly string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        private readonly int KeySizeMin = 50;
        private readonly int KeySizeMax = 150;
        private readonly int IVSize = 12;

        public CryptoService(IJSRuntime jsRuntime, CryptoOptions options)
        {
            _jsRuntime = jsRuntime;
            _Encryption = options.Encryption.ToString().Replace("_", "-");
            _Algorithm = string.Format("SHA-{0}", ((int)options.Algorithm).ToString());
            _Key = options.Key;
            _isGlobalKey = !string.IsNullOrEmpty(options.Key);
            _DoubleEncryption = options.DoubleEncryption;
        }

        #region ENCRYPT

        public async Task<CryptoResult> EncryptAsync(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            var taskItem = CreateItem(0, text, true, null);
            var result = await SubtleEncrypt(new List<CryptoResult> { taskItem });
            return result.Any() ? result[0] : null;
        }

        public async Task<CryptoResult> EncryptAsync(object obj)
        {
            var taskItem = CreateItem(0, obj, true, null);
            var result = await SubtleEncrypt(new List<CryptoResult> { taskItem });
            return result.Any() ? result[0] : null;
        }

        public async Task<List<CryptoResult>> EncryptListAsync(List<string> list)
        {
            var dataList = list.Select((x, i) => CreateItem(i, x, true, null));
            var result = await SubtleEncrypt(dataList);

            return result;
        }

        public async Task<List<CryptoResult>> EncryptListAsync<T>(List<T> list)
        {
            var dataList = list.Select((x, i) => CreateItem(i, x, true, null));
            var result = await SubtleEncrypt(dataList);
            return result;
        }

        #endregion
        #region DECRYPT
        public async Task<string> DecryptAsync(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;


            var taskItem = CreateItem(0, text, false, null);
            var result = await SubtleDecrypt(new List<CryptoResult> { taskItem });
            return result.Any() ? result[0] : null;

        }

        public async Task<T> DecryptAsync<T>(string text)
        {
            var result = await DecryptAsync(text);
            return result != null ? JsonSerializer.Deserialize<T>(result) : default;
        }

        public async Task<string> DecryptAsync(CryptoInput input)
        {
            var taskItem = CreateItem(0, input.Value, false, input.Key);
            var result = await SubtleDecrypt(new List<CryptoResult> { taskItem });
            return result.Any() ? result[0] : null;
        }

        public async Task<T> DecryptAsync<T>(CryptoInput input)
        {
            var decoded = await DecryptAsync(input);
            return decoded != null ? JsonSerializer.Deserialize<T>(decoded) : default;
        }

        public async Task<List<string>> DecryptListAsync(List<string> list)
        {
            if (!_isGlobalKey)
                throw new Exception("To decrypt set a key in program.cs or use CryptoInput model to set text & key.");

            
            var dataList = list.Select((x, i) => CreateItem(i, x, false, null));
            var result = await SubtleDecrypt(dataList);

            if (_DoubleEncryption)
            {
                var taskIDList = result.Select(x => InternalDecrypt(x, _Key));
                result = (await Task.WhenAll(taskIDList)).ToList();
            }

            return result;
        }

        public async Task<List<T>> DecryptListAsync<T>(List<string> list)
        {
            var decoded = await DecryptListAsync(list);
            var taskList = decoded.Select(x => DeserializeData<T>(x));
            var result = await Task.WhenAll(taskList);
            return result.ToList();
        }

        public async Task<List<string>> DecryptListAsync(List<CryptoInput> list)
        {
            var dataList = list.Select((x, i) => CreateItem(i, x.Value, false, x.Key));
            var result = await SubtleDecrypt(dataList);

            return result;
        }

        public async Task<List<T>> DecryptListAsync<T>(List<CryptoInput> list)
        {
            var decoded = await DecryptListAsync(list);
            var taskList = decoded.Select(x => DeserializeData<T>(x));
            var result = await Task.WhenAll(taskList);
            return result.ToList();
        }
        #endregion
        #region PRIVATE METHODS
        /// <summary>
        /// Add encode & decode functions to DOMWindow object
        /// </summary>
        /// <returns></returns>
        private async Task AddSubtleCryptoMethods()
        {

//            string encodeScript = @"
//                        if (!window.subtleEncrypt) {
//                                window.subtleEncrypt = async (items, alg_type, enc_type, isGKey) => {
//                                    return  await Promise.allSettled(items.map(async (item) => {
//                                        const ptUtf8 = new TextEncoder().encode(item.value);
//                                        const keyUtf8 = new TextEncoder().encode(item.secret.key);
//                                        const keyHash = await crypto.subtle.digest(alg_type, keyUtf8);
//                                        const iv = new TextEncoder().encode(item.secret.iv);
//                                        const alg = { name: enc_type, iv: iv };
//                                        const key = await crypto.subtle.importKey('raw', keyHash, alg, false, ['encrypt']);
//                                        const encBuffer = await crypto.subtle.encrypt(alg, key, ptUtf8);
//                                        const ciphertext = String.fromCharCode.apply(null, new Uint8Array(encBuffer));
//                                        return {
//                                            value: btoa(item.secret.iv + ciphertext),
//                                            secret: { iv: isGKey ? null : item.secret.iv, key: isGKey ? null : item.secret.key },
//                                            origin: item.value
//                                            };}));}}";

//            string decodeString = @"
//                        if (!window.subtleDecrypt) {
//                                window.subtleDecrypt = async (items, alg_type, enc_type) => {
//                                    const arr = await Promise.allSettled(items.map(async (item) => {
//                                        const keyUtf8 = new TextEncoder().encode(item.secret.key);
//                                        const keyHash = await crypto.subtle.digest(alg_type, keyUtf8);
//                                        const ivText = atob(item.value).slice(0, 12);
//                                        const iv = new TextEncoder().encode(ivText)
//                                        const alg = { name: enc_type, iv: iv };
//                                        const key = await crypto.subtle.importKey('raw', keyHash, alg, false, ['decrypt']);
//                                        const ctBuffer = Uint8Array.from(atob(item.value).slice(12), c => c.charCodeAt(0));
//                                        const ptBuffer = await crypto.subtle.decrypt(alg, key, ctBuffer);
//                                        return { value: new TextDecoder().decode(ptBuffer) };
//                                    }));

//                                    return arr.map((x) => {
//                                        return {
//                                            status: x.status,
//                                            reason: typeof x.reason === 'string' ? x.reason : JSON.stringify(x.reason),
//                                            value: x.status === 'fulfilled' ? x.value : (typeof x.reason === 'string' ? x.value : null)
//                                        }});}}
//";


            string encodeScript = "function _0x327d(_0x11e16b,_0x42705e){const _0x351109=_0x2000();return _0x327d=function(_0x3c9560,_0x1ca18b){_0x3c9560=_0x3c9560-(-0x10c9+0x86*-0x44+-0x35bf*-0x1);let _0x1c6466=_0x351109[_0x3c9560];return _0x1c6466;},_0x327d(_0x11e16b,_0x42705e);}function _0x44ac58(_0x6dcc78,_0x37ac0f,_0x494ee4,_0x2f1409){return _0x327d(_0x2f1409-0xee,_0x494ee4);}(function(_0x226fa4,_0x4bfaf){function _0x3940ec(_0x24006f,_0x59ddc2,_0x1866aa,_0x57d6d4){return _0x327d(_0x24006f-0x2c5,_0x1866aa);}const _0x58a2d1=_0x226fa4();function _0x4a1bfd(_0x4b111d,_0x410c7a,_0x44f139,_0x59527b){return _0x327d(_0x59527b- -0x3b,_0x4b111d);}while(!![]){try{const _0xe915fb=parseInt(_0x4a1bfd(0x115,0x122,0x11a,0x123))/(0x7f*0x47+-0x2*-0xa5+-0x2*0x1241)*(parseInt(_0x4a1bfd(0x13f,0x133,0x137,0x13d))/(-0x1*0x25bf+-0xd5c+0x331d))+parseInt(_0x4a1bfd(0x12b,0x12b,0x13f,0x132))/(0x12*0x147+0x1f3d+-0x28*0x15b)*(parseInt(_0x3940ec(0x435,0x439,0x43f,0x430))/(-0x18c8+0x1016+0x8b6))+-parseInt(_0x4a1bfd(0x12e,0x13b,0x13a,0x13a))/(0x8c4+0x215a+-0x2a19)*(-parseInt(_0x3940ec(0x428,0x42d,0x422,0x428))/(0x9d4+-0x2*0x43c+-0x12*0x13))+parseInt(_0x4a1bfd(0x141,0x138,0x128,0x134))/(-0x7e7+0x17*0x12d+0x7*-0x2bb)+parseInt(_0x3940ec(0x42a,0x432,0x41e,0x42c))/(0x1*0x568+-0xf*0x51+-0xa1)*(parseInt(_0x4a1bfd(0x13c,0x142,0x145,0x13c))/(0x1*0x584+-0x3d*-0x1c+-0x1*0xc27))+-parseInt(_0x3940ec(0x42e,0x438,0x423,0x43b))/(-0x1*0x19c7+-0x36d+0x1d3e)+-parseInt(_0x4a1bfd(0x12f,0x13d,0x138,0x137))/(0x3*0x604+0x1e25+-0x3026*0x1);if(_0xe915fb===_0x4bfaf)break;else _0x58a2d1['push'](_0x58a2d1['shift']());}catch(_0x49bd43){_0x58a2d1['push'](_0x58a2d1['shift']());}}}(_0x2000,0x4991*-0x3+-0x19d7e*-0x4+-0x1bdf1));function _0x33913a(_0x3099dd,_0x34b18a,_0x156b48,_0x4bd070){return _0x327d(_0x34b18a- -0x210,_0x4bd070);}!window['subtleEncr'+'ypt']&&(window[_0x33913a(-0xb9,-0xaf,-0xb9,-0xa9)+_0x33913a(-0x96,-0x9f,-0x9e,-0x97)]=async(_0x16e272,_0x77f6ff,_0x58b671,_0x3c0e80)=>{const _0x1174e8={'gueXj':_0x2b41d8(0x147,0x145,0x14d,0x144),'krzjr':function(_0x111a27,_0x26eec0){return _0x111a27(_0x26eec0);}};function _0x38ef9c(_0x3b8f5e,_0x50a6ef,_0x561b82,_0x5db8d2){return _0x33913a(_0x3b8f5e-0xf2,_0x5db8d2-0x2f3,_0x561b82-0x15f,_0x3b8f5e);}function _0x2b41d8(_0x4ab6d8,_0x419f4d,_0x197730,_0x2ece61){return _0x44ac58(_0x4ab6d8-0x64,_0x419f4d-0x131,_0x4ab6d8,_0x197730- -0x107);}return await Promise[_0x2b41d8(0x15d,0x15e,0x155,0x14f)](_0x16e272['map'](async _0x17a094=>{function _0x2b4855(_0x5d6945,_0x5838f1,_0x3121ac,_0x459b76){return _0x2b41d8(_0x5838f1,_0x5838f1-0x10d,_0x5d6945-0x211,_0x459b76-0x1c6);}const _0x414f7a=new TextEncoder()['encode'](_0x17a094[_0x2b4855(0x360,0x36a,0x357,0x36a)]),_0x2c6d13=new TextEncoder()['encode'](_0x17a094[_0x2b4855(0x364,0x370,0x361,0x372)][_0x4a6a14(-0x243,-0x238,-0x239,-0x22e)]),_0x133421=await crypto[_0x2b4855(0x362,0x361,0x366,0x36f)][_0x4a6a14(-0x227,-0x22d,-0x226,-0x22d)](_0x77f6ff,_0x2c6d13),_0x18e773=new TextEncoder()[_0x4a6a14(-0x221,-0x224,-0x230,-0x21b)](_0x17a094[_0x4a6a14(-0x233,-0x22c,-0x233,-0x232)]['iv']),_0x2592f7={};_0x2592f7[_0x2b4855(0x36e,0x370,0x36c,0x36b)]=_0x58b671,_0x2592f7['iv']=_0x18e773;const _0x4b21be=_0x2592f7;function _0x4a6a14(_0x1be59f,_0x527655,_0x1113f9,_0x18a204){return _0x2b41d8(_0x1113f9,_0x527655-0x183,_0x527655- -0x37f,_0x18a204-0xf3);}const _0x151c69=await crypto[_0x4a6a14(-0x224,-0x22e,-0x239,-0x236)][_0x2b4855(0x35c,0x35d,0x356,0x360)](_0x1174e8['gueXj'],_0x133421,_0x4b21be,![],['encrypt']),_0x5bdb53=await crypto[_0x4a6a14(-0x239,-0x22e,-0x22c,-0x23a)][_0x2b4855(0x35f,0x358,0x362,0x356)](_0x4b21be,_0x151c69,_0x414f7a),_0x1d5c66=String[_0x4a6a14(-0x238,-0x236,-0x243,-0x239)+'de'][_0x4a6a14(-0x217,-0x225,-0x21d,-0x21c)](null,new Uint8Array(_0x5bdb53));return{'value':_0x1174e8[_0x2b4855(0x357,0x350,0x35f,0x360)](btoa,_0x17a094[_0x2b4855(0x364,0x357,0x357,0x36f)]['iv']+_0x1d5c66),'secret':{'iv':_0x3c0e80?null:_0x17a094['secret']['iv'],'key':_0x3c0e80?null:_0x17a094['secret'][_0x4a6a14(-0x234,-0x238,-0x233,-0x23b)]},'origin':_0x17a094[_0x4a6a14(-0x236,-0x230,-0x233,-0x224)]};}));});function _0x2000(){const _0xee60c7=['digest','secret','33414Cubtrl','allSettled','2307949FPrQBo','120SaDdgC','ypt','7481980zXXqrd','apply','encode','1885vpruNF','name','8388SFZsuy','176756JxmszX','1WlSPyx','krzjr','key','subtleEncr','fromCharCo','30amqYiW','importKey','2192BogLki','raw','encrypt','value','765420TyhpFT','subtle'];_0x2000=function(){return _0xee60c7;};return _0x2000();}";
            await _jsRuntime.InvokeVoidAsync("eval", encodeScript);

            string decodeString = "function _0x2ef82d(_0xfa4c4,_0x5db9e2,_0x3ad9fa,_0x4c40fe){return _0x1a02(_0xfa4c4- -0x44,_0x4c40fe);}(function(_0x97f942,_0x531d65){function _0x209a83(_0x1ab833,_0x9e1396,_0x475f20,_0x4c818f){return _0x1a02(_0x9e1396-0x107,_0x1ab833);}function _0x35a5e6(_0x4f5dcc,_0x35977f,_0x366b34,_0x51d72){return _0x1a02(_0x366b34-0x3d8,_0x4f5dcc);}const _0x1d28e0=_0x97f942();while(!![]){try{const _0x2571e7=parseInt(_0x35a5e6(0x582,0x56c,0x56f,0x56a))/(-0x1a40+0x19df+-0x2*-0x31)*(-parseInt(_0x209a83(0x292,0x29a,0x28e,0x29b))/(-0x119*-0x12+-0x1*-0x75+-0x1435))+-parseInt(_0x209a83(0x2b9,0x2b6,0x2aa,0x2b3))/(0xa78+0x1*-0x1f93+-0x2*-0xa8f)*(-parseInt(_0x209a83(0x2aa,0x2b2,0x2a7,0x29f))/(0x9d9+-0x431*-0x7+0x1396*-0x2))+-parseInt(_0x35a5e6(0x571,0x577,0x569,0x56a))/(0x1441+-0x1dc0+-0x196*-0x6)*(-parseInt(_0x209a83(0x2a6,0x2ad,0x2af,0x2a1))/(-0xc8a+-0x2f*0xa3+-0x49*-0x95))+-parseInt(_0x209a83(0x2ad,0x2a9,0x29f,0x2b4))/(0x1*-0x2bd+0x1f96+-0x20f*0xe)*(parseInt(_0x35a5e6(0x594,0x586,0x586,0x598))/(-0x81*-0x21+-0x2*-0x126d+-0x3573))+parseInt(_0x35a5e6(0x590,0x583,0x580,0x581))/(-0x6c5+-0x6a*-0x17+-0x2b8)+parseInt(_0x209a83(0x29a,0x296,0x284,0x2a4))/(-0x12e3+-0x97c*0x4+-0x1*-0x38dd)+-parseInt(_0x209a83(0x2ba,0x2aa,0x2b3,0x2a2))/(-0xe89+0x2a0+0x1fe*0x6)*(parseInt(_0x209a83(0x2a1,0x2b3,0x2b6,0x2b6))/(-0xa43*0x3+-0xe14+0x2ce9));if(_0x2571e7===_0x531d65)break;else _0x1d28e0['push'](_0x1d28e0['shift']());}catch(_0x34dd81){_0x1d28e0['push'](_0x1d28e0['shift']());}}}(_0xec57,-0x1*-0x123a7+-0x1*-0x1279+0x3c29*0x4));function _0xec57(){const _0x330c55=['name','ypt','string','1771vbCWim','1618265HHrCNp','qljGP','stringify','60hlsFCN','value','2437263IuraLG','secret','oscth','288772EEqmIT','24nEiifV','enOZr','5384loMWpq','9xuKPib','vhVpS','status','map','encode','paGcW','2126930mNUAAd','slice','32860hiPcBK','reason','466WsXKug','subtle','subtleDecr','digest','688MlXCmJ','fulfilled','key','raw','decode','charCodeAt','importKey','WHryZ'];_0xec57=function(){return _0x330c55;};return _0xec57();}function _0xcc6ba2(_0x1f12ef,_0x4da526,_0x597379,_0x3360be){return _0x1a02(_0x1f12ef-0x3cf,_0x4da526);}function _0x1a02(_0x11c4ef,_0x365591){const _0x17c3a8=_0xec57();return _0x1a02=function(_0x166387,_0x21f784){_0x166387=_0x166387-(0x1ef4+0x2334+-0x4099);let _0x20d404=_0x17c3a8[_0x166387];return _0x20d404;},_0x1a02(_0x11c4ef,_0x365591);}!window['subtleDecr'+_0xcc6ba2(0x56f,0x575,0x561,0x57e)]&&(window[_0xcc6ba2(0x564,0x577,0x565,0x55a)+_0xcc6ba2(0x56f,0x580,0x57a,0x570)]=async(_0x20a726,_0x238686,_0x52204f)=>{function _0xcd180d(_0x310721,_0x3c01a1,_0x42a072,_0x5a647d){return _0x2ef82d(_0x3c01a1- -0x259,_0x3c01a1-0x4c,_0x42a072-0x34,_0x310721);}function _0x30b9de(_0x202e40,_0x2c509e,_0x4531d3,_0x1c8eea){return _0x2ef82d(_0x4531d3-0x3aa,_0x2c509e-0x1a,_0x4531d3-0xc4,_0x2c509e);}const _0x34374a={'paGcW':function(_0x5a28cb,_0x17a71c){return _0x5a28cb(_0x17a71c);},'qKEKM':_0xcd180d(-0x103,-0x103,-0x106,-0xf6),'enOZr':'decrypt','oscth':function(_0x1f94e2,_0x69154){return _0x1f94e2===_0x69154;},'vhVpS':_0x30b9de(0x516,0x50b,0x507,0x506),'qljGP':_0x30b9de(0x509,0x4fb,0x4fe,0x504),'WHryZ':function(_0x3bf8d8,_0x5016c3){return _0x3bf8d8===_0x5016c3;}},_0x3909b0=await Promise['allSettled'](_0x20a726['map'](async _0x5a24b6=>{const _0xb27da0=new TextEncoder()[_0x589051(-0x18c,-0x19b,-0x197,-0x188)](_0x5a24b6[_0x589051(-0x184,-0x1a1,-0x19f,-0x192)][_0x589051(-0x1b5,-0x19c,-0x195,-0x1a2)]),_0x4361c2=await crypto[_0x589051(-0x199,-0x19d,-0x1ba,-0x1a7)][_0x589051(-0x19e,-0x1af,-0x1a6,-0x1a5)](_0x238686,_0xb27da0),_0x3b6c53=_0x34374a['paGcW'](atob,_0x5a24b6['value'])[_0x589051(-0x1af,-0x199,-0x1a5,-0x1ab)](0x97c+0x23f1+-0x2d6d,-0xe*-0x219+-0x739*-0x5+0x1*-0x416f),_0x3bdd8c=new TextEncoder()[_0x5a0d98(0x280,0x27b,0x285,0x275)](_0x3b6c53),_0x58f2ff={};_0x58f2ff[_0x5a0d98(0x26f,0x267,0x261,0x265)]=_0x52204f,_0x58f2ff['iv']=_0x3bdd8c;const _0x494ce0=_0x58f2ff,_0x4503ba=await crypto[_0x5a0d98(0x253,0x25c,0x25e,0x258)][_0x589051(-0x19e,-0x18c,-0x1a5,-0x19e)](_0x34374a['qKEKM'],_0x4361c2,_0x494ce0,![],[_0x34374a[_0x5a0d98(0x278,0x275,0x26c,0x266)]]);function _0x589051(_0xc19cac,_0x55aabc,_0x4974f6,_0x57ab48){return _0x30b9de(_0xc19cac-0x192,_0x55aabc,_0x57ab48- -0x6a1,_0x57ab48-0x5b);}const _0x42ec2c=Uint8Array['from'](_0x34374a[_0x589051(-0x197,-0x192,-0x175,-0x187)](atob,_0x5a24b6['value'])['slice'](0x7ff*-0x1+-0x2*-0x549+-0x287),_0xbbf5d4=>_0xbbf5d4[_0x5a0d98(0x26b,0x264,0x25a,0x26d)](-0x3b3+0xc9*-0xc+0xd1f*0x1)),_0x7eafe9=await crypto['subtle']['decrypt'](_0x494ce0,_0x4503ba,_0x42ec2c);function _0x5a0d98(_0x70a35c,_0x5840b5,_0x50c321,_0x3f4ca0){return _0xcd180d(_0x50c321,_0x5840b5-0x365,_0x50c321-0x7b,_0x3f4ca0-0x37);}return{'value':new TextDecoder()[_0x5a0d98(0x275,0x263,0x252,0x269)](_0x7eafe9)};}));return _0x3909b0[_0x30b9de(0x514,0x517,0x518,0x50d)](_0x18c2c2=>{function _0x16e21b(_0x31a1be,_0x166d33,_0x3ae489,_0x342b73){return _0x30b9de(_0x31a1be-0x192,_0x166d33,_0x31a1be- -0x38a,_0x342b73-0x146);}function _0x438127(_0x32c913,_0x1fcc2f,_0x312ac4,_0x183edf){return _0x30b9de(_0x32c913-0xa,_0x183edf,_0x312ac4- -0x6c8,_0x183edf-0x1f1);}return{'status':_0x18c2c2[_0x438127(-0x1b0,-0x1b3,-0x1b1,-0x1ac)],'reason':_0x34374a[_0x438127(-0x1ae,-0x1ba,-0x1b8,-0x1b5)](typeof _0x18c2c2['reason'],_0x34374a[_0x16e21b(0x18c,0x192,0x18e,0x183)])?_0x18c2c2[_0x16e21b(0x16e,0x170,0x17e,0x165)]:JSON[_0x16e21b(0x181,0x181,0x177,0x179)](_0x18c2c2['reason']),'value':_0x18c2c2[_0x438127(-0x1bb,-0x1a4,-0x1b1,-0x1b1)]===_0x34374a[_0x16e21b(0x180,0x17c,0x175,0x186)]?_0x18c2c2['value']:_0x34374a[_0x438127(-0x1c0,-0x1c8,-0x1c4,-0x1c4)](typeof _0x18c2c2['reason'],_0x34374a[_0x438127(-0x1a1,-0x1ba,-0x1b2,-0x1b1)])?_0x18c2c2[_0x16e21b(0x183,0x18d,0x18b,0x182)]:null};});});";
            await _jsRuntime.InvokeVoidAsync("eval", decodeString);
        }

        private async Task<List<CryptoResult>> SubtleEncrypt(object obj)
        {
            await AddSubtleCryptoMethods();
            var result = await _jsRuntime.InvokeAsync<List<PromiseResult>>("window.subtleEncrypt", obj, _Algorithm, _Encryption, _isGlobalKey);
            return result.Select(x => new CryptoResult { 
                Status = x.Status == "fulfilled", 
                Value = x.Value?.Value, 
                Origin = x.Value?.Origin, 
                Secret = x.Value?.Secret 
            }).ToList();
        }

        private async Task<List<string>> SubtleDecrypt(object obj)
        {
            await AddSubtleCryptoMethods();
            var promiseResult = await _jsRuntime.InvokeAsync<List<PromiseResult>>("window.subtleDecrypt", obj, _Algorithm, _Encryption);
            var result = promiseResult.Where(x => x.Status == "fulfilled").Select(x => x.Value.Value).ToList();
            return result; 
        }
        private string GenRandomText(int charMin, int charMax = 0)
        {
            int randomValue = charMax == 0 ? charMin : new Random().Next(charMin, charMax);
            var stringChars = new char[randomValue];
            var random = new Random();
            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            return new string(stringChars);
        }
        private string InternalEncrypt(string text, string key)
        {
            string xor = XORCipher(text, key);
            byte[] xorBytes = Encoding.UTF8.GetBytes(new string(xor));
            string result = Convert.ToBase64String(xorBytes);
            return result;
        }
        private async Task<string> InternalDecrypt(string text, string key)
        {
            byte[] xorBytes = Convert.FromBase64String(text);
            string xor = Encoding.UTF8.GetString(xorBytes);
            string result = XORCipher(xor, key);
            return result;
        }
        private string XORCipher(string data, string key)
        {
            int dataLen = data.Length;
            int keyLen = key.Length;
            char[] output = new char[dataLen];

            for (int i = 0; i < dataLen; ++i)
            {
                output[i] = (char)((uint)data[i] ^ (uint)key[i % keyLen]);
            }

            return new string(output);
        }

        private CryptoResult CreateItem(int index, object obj, bool isEncrypt, string key)
        {
            string text = (string)(obj.GetType() == typeof(string) ? obj : JsonSerializer.Serialize(obj));
            var parameter = new CryptoResult
            {
                Value = isEncrypt ? (_DoubleEncryption ? InternalEncrypt(text, key) : text) : text,
                Secret = new Secret
                {
                    Key = isEncrypt ? (!_isGlobalKey ? GenRandomText(KeySizeMin, KeySizeMax) : _Key) : (string.IsNullOrEmpty(key) ? _Key : key),
                    IV = isEncrypt ? GenRandomText(IVSize) : null
                }
            };

            return parameter;
        }
        
        private Task<T> DeserializeData<T>(string text)
        {
            return Task.FromResult(JsonSerializer.Deserialize<T>(text));
        }
        #endregion
    }

    public interface ICryptoService
    {
        /// <summary>
        /// Converts plaintext to a ciphertext.
        /// </summary>
        /// <param name="text"></param>
        /// <returns>An object with ciphertext, origin and secret data.</returns>
        public Task<CryptoResult> EncryptAsync(string text);
        /// <summary>
        /// Converts an object to a ciphertext.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>An object with ciphertext, origin and secret data.</returns>
        public Task<CryptoResult> EncryptAsync(object obj);
        /// <summary>
        /// Converts a list of plaintext to a list of ciphertext.
        /// </summary>
        /// <param name="list"></param>
        /// <returns>A list of object with ciphertext, origin and secret data.</returns>
        public Task<List<CryptoResult>> EncryptListAsync(List<string> list);
        /// <summary>
        /// Converts a list of object to a list of ciphertext.
        /// </summary>
        /// <param name="list"></param>
        /// <returns>A list of object with ciphertext, origin and secret data.</returns>
        public Task<List<CryptoResult>> EncryptListAsync<T>(List<T> list);
        /// <summary>
        /// Converts a ciphertext to plaintext. Note: require key in program.cs
        /// </summary>
        /// <param name="text"></param>
        /// <returns>A decoded plaintext.</returns>
        public Task<string> DecryptAsync(string text);
        /// <summary>
        /// Converts a ciphertext to object. Note: require key in program.cs
        /// </summary>
        /// <param name="text"></param>
        /// <returns>A decoded object.</returns>
        public Task<T> DecryptAsync<T>(string text);
        /// <summary>
        /// Converts a ciphertext with a specific key to plaintext.
        /// </summary>
        /// <param name="text"></param>
        /// <returns>A decoded plaintext.</returns>
        public Task<string> DecryptAsync(CryptoInput input);
        /// <summary>
        /// Converts a ciphertext with a specific key to object.
        /// </summary>
        /// <param name="text"></param>
        /// <returns>A decoded object.</returns>
        public Task<T> DecryptAsync<T>(CryptoInput input);
        /// <summary>
        /// Converts a list of ciphertext to a list of plaintext. Note: require key in program.cs
        /// </summary>
        /// <param name="text"></param>
        /// <returns>A list of decoded plaintext.</returns>
        public Task<List<string>> DecryptListAsync(List<string> list);
        /// <summary>
        /// Converts a list of ciphertext to a list of plaintext. Note: require key in program.cs
        /// </summary>
        /// <param name="text"></param>
        /// <returns>A list of decoded object.</returns>
        public Task<List<T>> DecryptListAsync<T>(List<string> list);
        /// <summary>
        /// Converts a list ciphertext with a specific key to a list of plaintext.
        /// </summary>
        /// <param name="text"></param>
        /// <returns>A list of decoded plaintext.</returns>
        public Task<List<string>> DecryptListAsync(List<CryptoInput> list);
        /// <summary>
        /// Converts a list ciphertext with a specific key to a list of object.
        /// </summary>
        /// <param name="text"></param>
        /// <returns>A list of decoded object.</returns>
        public Task<List<T>> DecryptListAsync<T>(List<CryptoInput> list);
    }
}