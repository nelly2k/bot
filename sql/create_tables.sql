create database bot;
go
use bot;

--create table Pairs(
--	id int not null identity constraint pk_pairs primary key,
--	altname nvarchar(15),
--	aclass_base  nvarchar(30),
--	base  nvarchar(15),
--	aclass_quote nvarchar(30),
--    quote nvarchar(15),
--    lot nvarchar(15),
--    pair_decimals int,
--    lot_decimals int,
--    lot_multiplier int,
--    margin_call int,
--    margin_stop int
--)

create table trades(
	id int not null identity constraint pk_trades primary key,
	altname nvarchar(15),
	price decimal(18,10),
	volume decimal(18,10),
	tradeTime datetime,
	buy_sell char(1),
	market_limit char(1),
	misc NVARCHAR(100)
)


create table config(
	platform nvarchar(50),
	name nvarchar(50),
	value nvarchar(100)
)


insert into config values ('kraken', 'load_interval_minutes',3)
insert into config values ('kraken', 'analyse_load_hours', 12)
insert into config values ('kraken', 'analyse_group_period_minutes',3)
insert into config values ('kraken', 'analyse_treshold_minutes',10)
insert into config values ('kraken', 'analyse_macd_slow',20)
insert into config values ('kraken', 'analyse_macd_slow_threshold',0.00)
insert into config values ('kraken', 'analyse_macd_fast',10)
insert into config values ('kraken', 'analyse_macd_signal',5)
insert into config values ('kraken', 'analyse_rsi_ema_periods',14)
insert into config values ('kraken', 'analyse_rsi_low',35)
insert into config values ('kraken', 'analyse_rsi_high',70)
insert into config values ('kraken', 'api_key','')
insert into config values ('kraken', 'api_secret','')
insert into config values ('kraken', 'max_missed_sells',3)
insert into config values ('kraken', 'pair_percent','ETHUSD|80')
insert into config values ('kraken', 'base_currency','ZUSD')
insert into config values ('kraken', 'analyse_amcd_group_perios_minutes_slow',20)
insert into config values ('kraken', 'pair_load','ETHUSD')
insert into config values ('kraken', 'pair_load','XBTUSD')
insert into config values ('kraken', 'min_volume','ETHUSD|0.02');

ALTER table config
add constraint pk_config PRIMARY KEY (platform, name, value)


create table log(
	platform nvarchar(50),
	datetime datetime not null constraint df_log default (getdate()),
	status nvarchar(15) not null,
	event nvarchar(max)
)

create table lastEvent(
	platform nvarchar(50),
	name nvarchar(150) not null,
	datetime datetime,
	value nvarchar(MAX)
)
go


create table balance(
	platform nvarchar(50),
	name nvarchar(15),
	volume decimal(18,10),
	price decimal(18,10), 
	notSoldCounter int  constraint df_notSoldCounter default (0),
	notSoldDate DateTime,
	boughtDate Datetime constraint df_boughtDate default (getdate()),
	isDeleted bit constraint df_balance_isDelete default (0)
)

create table openOrder(
	
	platform nvarchar(50),
	altname nvarchar(15),
	id nvarchar(500),
	isDeleted bit constraint df_openOrder_isDelete default (0)
)

create table operation(
	id int not null primary key identity,
	platform  nvarchar(50),
	isDeleted bit constraint dg_operation_isDelted default (0)
)