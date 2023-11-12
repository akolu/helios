namespace Helios.Core.Services

module FusionSolar =

    type LoginParams =
        { userName: string; systemCode: string }

// let login (client: IHttpClient) (body: LoginParams) =
//     async {
//         let! response =
//             (client.Post "https://eu5.fusionsolar.huawei.com/thirdData/login" body)
//             |> Async.RunSynchronously

//         return response
//     }
