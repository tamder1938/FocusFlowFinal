-- ============================================================
-- Feature 12 Phase 1: Friends & Social Tables
-- Run in Supabase SQL Editor (once per project)
-- ============================================================

-- ── profiles (public user info) ──────────────────────────────
-- Auto-populated via trigger on auth.users insert
create table if not exists public.profiles (
    id          uuid primary key references auth.users(id) on delete cascade,
    username    text not null default '',
    email       text not null default '',
    avatar_url  text,
    updated_at  timestamptz not null default now()
);

alter table public.profiles enable row level security;

-- Everyone can read public profiles (needed for friend search)
create policy "profiles: public read"
    on public.profiles for select
    using (true);

-- Users can update their own profile
create policy "profiles: owner update"
    on public.profiles for update
    using (auth.uid() = id)
    with check (auth.uid() = id);

-- ── Trigger: create profile on user signup ────────────────────
create or replace function public.handle_new_user()
returns trigger as $$
begin
    insert into public.profiles (id, username, email)
    values (
        new.id,
        coalesce(new.raw_user_meta_data->>'username', split_part(new.email, '@', 1)),
        coalesce(new.email, '')
    )
    on conflict (id) do nothing;
    return new;
end;
$$ language plpgsql security definer;

drop trigger if exists on_auth_user_created on auth.users;
create trigger on_auth_user_created
    after insert on auth.users
    for each row execute procedure public.handle_new_user();

-- ── friendships ──────────────────────────────────────────────
create table if not exists public.friendships (
    id           uuid primary key default gen_random_uuid(),
    requester_id uuid not null references auth.users(id) on delete cascade,
    addressee_id uuid not null references auth.users(id) on delete cascade,
    status       text not null default 'pending', -- 'pending' | 'accepted' | 'declined'
    created_at   timestamptz not null default now(),
    updated_at   timestamptz not null default now(),
    constraint unique_friendship unique (requester_id, addressee_id),
    constraint no_self_friend check (requester_id <> addressee_id)
);

alter table public.friendships enable row level security;

-- Requester can see and manage their sent requests
create policy "friendships: requester access"
    on public.friendships for all
    using (auth.uid() = requester_id)
    with check (auth.uid() = requester_id);

-- Addressee can see requests directed to them
create policy "friendships: addressee read"
    on public.friendships for select
    using (auth.uid() = addressee_id);

-- Addressee can accept/decline (update status)
create policy "friendships: addressee update"
    on public.friendships for update
    using (auth.uid() = addressee_id)
    with check (auth.uid() = addressee_id);

-- ── Indexes ──────────────────────────────────────────────────
create index if not exists idx_friendships_requester on public.friendships(requester_id);
create index if not exists idx_friendships_addressee on public.friendships(addressee_id);
create index if not exists idx_friendships_status    on public.friendships(status);
create index if not exists idx_profiles_email        on public.profiles(email);
