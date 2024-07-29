CREATE TABLE IF NOT EXISTS public."user" (
	id uuid DEFAULT gen_random_uuid() NOT NULL,
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
	created_at timestamptz NOT NULL,
	updated_at timestamptz NOT NULL,
	is_removed bool DEFAULT false NOT NULL,
	CONSTRAINT user_pk PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS user_email_idx ON public."user" USING btree (email);

CREATE TABLE IF NOT EXISTS public.friends (
	id uuid NOT NULL,
	user_id uuid NULL,
	friend_id uuid NULL,
	created_at timestamp NOT NULL,
	is_removed boolean NOT NULL,
	updated_at timestamp NOT NULL,
	CONSTRAINT friends_pk PRIMARY KEY (id),
	CONSTRAINT user_friend_fk FOREIGN KEY (user_id) REFERENCES public."user"(id),
	CONSTRAINT user_friend_user_fk FOREIGN KEY (friend_id) REFERENCES public."user"(id)
);
