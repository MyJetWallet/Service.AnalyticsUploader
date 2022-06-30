﻿using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using Service.AnalyticsUploader.Domain;
using Service.AnalyticsUploader.Domain.Models.AnaliticsEvents;
using Service.Bitgo.WithdrawalProcessor.Domain.Models;
using Service.ClientProfile.Grpc;
using Service.PersonalData.Grpc;

namespace Service.AnalyticsUploader.Job
{
	public class WithdrawalHandleJob : MessageHandleJobBase
	{
		private readonly ILogger<WithdrawalHandleJob> _logger;

		public WithdrawalHandleJob(ILogger<WithdrawalHandleJob> logger,
			ISubscriber<IReadOnlyList<Withdrawal>> subscriber,
			IAppsFlyerSender sender,
			IPersonalDataServiceGrpc personalDataServiceGrpc,
			IClientProfileService clientProfileService) :
				base(logger, personalDataServiceGrpc, clientProfileService, sender)
		{
			_logger = logger;
			subscriber.Subscribe(HandleEvent);
		}

		private async ValueTask HandleEvent(IReadOnlyList<Withdrawal> messages)
		{
			foreach (Withdrawal message in messages)
			{
				if (message.Status != WithdrawalStatus.Success)
					continue;

				string clientId = message.ClientId;

				_logger.LogInformation("Handle Withdrawal message, clientId: {clientId}.", message.ClientId);

				string network = message.Blockchain;
				string assetSymbol = message.AssetSymbol;
				decimal amount = message.Amount;

				if (message.IsInternal)
				{
					string destinationClientId = message.DestinationClientId;
					string destinationCuid = await GetExternalClientId(destinationClientId);
					if (destinationCuid == null)
					{
						_logger.LogError("DestinationCuid is null, skip uploading");
						continue;
					}

					await SendMessage(clientId, new SendTransferByWalletInternalEvent
					{
						Amount = amount,
						Currency = assetSymbol,
						Receiver = destinationCuid,
						Network = network
					});

					await SendMessage(destinationCuid, new RecieveTransferFromInternalWalletEvent
					{
						Amount = amount,
						Currency = assetSymbol,
						Sender = clientId,
						Network = network
					});
				}
				else
				{
					await SendMessage(clientId, new SendTransferByWalletExternalEvent
					{
						Amount = amount,
						Currency = assetSymbol,
						Receiver = message.ToAddress,
						Network = network
					});
				}
			}
		}
	}
}