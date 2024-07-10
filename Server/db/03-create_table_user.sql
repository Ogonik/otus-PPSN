CREATE TABLE public."user" (
	id uuid GENERATED ALWAYS AS IDENTITY NOT NULL,
	first_name varchar(256) NULL,
	last_name varchar(256) NULL,
	photo_link varchar(128) NULL,
	birth_date date NULL,
	sex int2 NULL,
	city varchar(64) NULL,
	interests varchar NULL,
	email varchar(128) NOT NULL,
	email_confirmed bool NULL,
	phone varchar(64) NULL,
	"password" varchar NOT NULL,
	CONSTRAINT user_pk PRIMARY KEY (id)
);
CREATE UNIQUE INDEX user_email_idx ON public."user" (email);
