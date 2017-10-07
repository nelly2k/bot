
create table Pairs(
	id int not null identity constraint pk_pairs primary key,
	altname nvarchar(15),
	aclass_base  nvarchar(30),
	base  nvarchar(15),
	aclass_quote nvarchar(30),
    quote nvarchar(15),
    lot nvarchar(15),
    pair_decimals int,
    lot_decimals int,
    lot_multiplier int,
    margin_call int,
    margin_stop int
)

create table Trades(
	id int not null identity constraint pk_trades primary key,
	altname nvarchar(15),
	price decimal,
	volume decimal,
	tradeTime datetime,
	buy_sell char(1),
	market_limit char(1),
	misc NVARCHAR(100)
)


create table lastid(
	altname nvarchar(15),
	time DATETIME constraint df_lastid_time default(getdate()),
	id nvarchar(15)
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
insert into config values ('kraken', 'analyse_macd_fast',10)
insert into config values ('kraken', 'analyse_macd_signal',5)
insert into config values ('kraken', 'analyse_rsi_ema_periods',14)
insert into config values ('kraken', 'analyse_rsi_low',35)
insert into config values ('kraken', 'analyse_rsi_high',70)

create table log(
	platform nvarchar(50),
	datetime datetime not null constraint df_log default (getdate()),
	status nvarchar(15) not null,
	event nvarchar(max)
)

