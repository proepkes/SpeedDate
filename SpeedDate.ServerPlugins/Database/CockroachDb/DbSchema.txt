create table accounts
(
	account_id bigserial not null
		constraint idx_16388_primary
			primary key,
	username varchar(45),
	password varchar(125),
	email varchar(125),
	token varchar(125),
	is_admin boolean,
	is_guest boolean,
	is_email_confirmed boolean
)
;

create unique index idx_16388_username_unique
	on accounts (username)
;

create unique index idx_16388_email_unique
	on accounts (email)
;

create table account_properties
(
	account_property_id bigserial not null
		constraint idx_16394_primary
			primary key,
	account_id bigint not null
		constraint fk_account_properties_accounts
			references accounts,
	prop_key varchar(45),
	prop_val varchar(300)
)
;

create index idx_16394_fk_account_properties_accounts_idx
	on account_properties (account_id)
;

create table email_confirmation_codes
(
	email varchar(125) not null
		constraint idx_16398_primary
			primary key,
	code varchar(45)
)
;

create table password_reset_codes
(
	email varchar(125) not null
		constraint idx_16401_primary
			primary key,
	code varchar(45)
)
;

create table profile_values
(
	account_id bigint not null
		constraint fk_profile_values_accounts1
			references accounts,
	value_key bigint not null,
	value_value text,
	constraint idx_16404_primary
		primary key (account_id, value_key)
)
;

create index idx_16404_fk_profile_values_accounts1_idx
	on profile_values (account_id)
;

