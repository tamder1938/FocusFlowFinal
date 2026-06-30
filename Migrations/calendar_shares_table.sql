-- ============================================================
-- Feature 12 Phase 2: Calendar Shares Table
-- Run in Supabase SQL Editor (once per project)
-- Requires: friends_social_tables.sql already applied
-- ============================================================

create table if not exists public.calendar_shares (
    id                   uuid primary key default gen_random_uuid(),
    owner_user_id        uuid not null references auth.users(id) on delete cascade,
    shared_with_user_id  uuid not null references auth.users(id) on delete cascade,
    permission           text not null default 'view', -- 'view' | 'sync'
    is_active            boolean not null default true,
    created_at           timestamptz not null default now(),
    constraint unique_calendar_share unique (owner_user_id, shared_with_user_id),
    constraint no_self_share check (owner_user_id <> shared_with_user_id)
);

alter table public.calendar_shares enable row level security;

-- Owner can manage their shares
create policy "calendar_shares: owner full access"
    on public.calendar_shares for all
    using (auth.uid() = owner_user_id)
    with check (auth.uid() = owner_user_id);

-- Recipient can see shares directed to them
create policy "calendar_shares: recipient read"
    on public.calendar_shares for select
    using (auth.uid() = shared_with_user_id);

-- calendar_events: allow read access for people with whom the calendar is shared
-- This extends the existing calendar_events RLS
create policy "calendar_events: shared read access"
    on public.calendar_events for select
    using (
        user_id in (
            select owner_user_id
            from public.calendar_shares
            where shared_with_user_id = auth.uid()
              and is_active = true
        )
    );

-- Indexes
create index if not exists idx_calendar_shares_owner     on public.calendar_shares(owner_user_id);
create index if not exists idx_calendar_shares_recipient on public.calendar_shares(shared_with_user_id);
create index if not exists idx_calendar_shares_active    on public.calendar_shares(is_active);
