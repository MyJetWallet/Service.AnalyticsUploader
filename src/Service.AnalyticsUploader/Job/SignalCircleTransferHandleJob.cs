﻿using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Circle.Models.Payments;
using MyJetWallet.Circle.Models.Payouts;
using MyJetWallet.Circle.Settings.NoSql;
using MyJetWallet.Circle.Settings.Services;
using Service.AnalyticsUploader.Domain;
using Service.AnalyticsUploader.Domain.Models.AnaliticsEvents;
using Service.Circle.Webhooks.Domain.Models;
using Service.ClientProfile.Grpc;
using Service.PersonalData.Grpc;

namespace Service.AnalyticsUploader.Job
{
	public class SignalCircleTransferHandleJob : MessageHandleJobBase
	{
		private readonly ILogger<SignalCircleTransferHandleJob> _logger;
		private readonly ICircleAssetMapper _circleAssetMapper;

		public SignalCircleTransferHandleJob(ILogger<SignalCircleTransferHandleJob> logger,
			ISubscriber<IReadOnlyList<SignalCircleTransfer>> subscriber,
			IAppsFlyerSender sender,
			IClientProfileService clientProfileService,
			IPersonalDataServiceGrpc personalDataServiceGrpc,
			ICircleAssetMapper circleAssetMapper) :
				base(logger, personalDataServiceGrpc, clientProfileService, sender)
		{
			_logger = logger;
			_circleAssetMapper = circleAssetMapper;
			subscriber.Subscribe(HandleEvent);
		}

		private async ValueTask HandleEvent(IReadOnlyList<SignalCircleTransfer> messages)
		{
			foreach (SignalCircleTransfer message in messages)
			{
				PaymentInfo paymentInfo = message.PaymentInfo;
				if (paymentInfo.Source.Type != "card" || paymentInfo.Status != PaymentStatus.Complete)
					continue;

				string clientId = message.ClientId;

				_logger.LogInformation("Handle SignalCircleTransfer message, clientId: {clientId}.", clientId);

				string brokerId = message.BrokerId;
				CircleAmount captureAmount = paymentInfo.CaptureAmount;
				CircleAmount amount = paymentInfo.Amount;

				IAnaliticsEvent analiticsEvent = new BuyFromCardCircleEvent
				{
					PaidAmount = captureAmount.Amount,
					PaidCurrency = GetAsset(brokerId, captureAmount.Currency),
					ReceivedAmount = amount.Amount,
					ReceivedCurrency = GetAsset(brokerId, amount.Currency),
					FirstTimeBuy = false //todo
				};

				await SendMessage(clientId, analiticsEvent);
			}
		}

		private string GetAsset(string brokerId, string circleAsset)
		{
			CircleAssetEntity asset = _circleAssetMapper.CircleAssetToAsset(brokerId, circleAsset);
			if (asset != null)
				return asset.AssetTokenSymbol;

			_logger.LogError("Unknown Circle asset: {asset}, brokerId: {brokerId}", circleAsset, brokerId);

			return null;
		}
	}
}