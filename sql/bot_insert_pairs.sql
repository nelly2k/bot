USE [bot]
GO

INSERT INTO [dbo].[Pairs]
           ([altname]
           ,[aclass_base]
           ,[base]
           ,[aclass_quote]
           ,[quote]
           ,[lot]
           ,[pair_decimals]
           ,[lot_decimals]
           ,[lot_multiplier]
           ,[margin_call]
           ,[margin_stop])
     VALUES
           (@altname
           ,@aclass_base
           ,@base
           ,@aclass_quote
           ,@quote
           ,@lot
           ,@pair_decimals
           ,@lot_decimals
           ,@lot_multiplier
           ,@margin_call
           ,@margin_stop)
GO


