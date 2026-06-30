-- Feature 12 Phase 4: Shared calendar events
-- Events added by one user into another user's calendar slot (requires sync permission)

create table if not exists public.shared_calendar_events (
    id                  uuid primary key default gen_random_uuid(),
    owner_user_id       uuid not null references auth.users(id) on delete cascade,
    created_by_user_id  uuid not null references auth.users(id) on delete cascade,
    title               text not null,
    start_at            timestamptz not null,
    end_at              timestamptz not null,
    color               text not null default '#6366F1',
    is_all_day          boolean not null default false,
    notes               text,
    is_deleted          boolean not null default false,
    updated_at          timestamptz not null default now()
);

-- Index for fast per-owner queries
create index if not exists idx_sce_owner     on public.shared_calendar_events(owner_user_id);
create index if not exists idx_sce_creator   on public.shared_calendar_events(created_by_user_id);
create index if not exists idx_sce_start     on public.shared_calendar_events(start_at);

alter table public.shared_calendar_events enable row level security;

-- Owner can read all events in their own calendar
create policy "owner_read" on public.shared_calendar_events
    for select using (auth.uid() = owner_user_id);

-- Creator can read events they created
create policy "creator_read" on public.shared_calendar_events
    for select using (auth.uid() = created_by_user_id);

-- Anyone with sync permission on that owner's calendar can insert
create policy "sync_insert" on public.shared_calendar_events
    for insert with check (
        auth.uid() = created_by_user_id
        and exists (
            select 1 from public.calendar_shares cs
            where cs.owner_user_id = shared_calendar_events.owner_user_id
              and cs.shared_with_user_id = auth.uid()
              and cs.permission = 'sync'
              and cs.is_active = true
        )
    );

-- Creator can update/soft-delete their own created events
create policy "creator_update" on public.shared_calendar_events
    for update using (auth.uid() = created_by_user_id);

-- Creator can hard-delete their own events
create policy "creator_delete" on public.shared_calendar_events
    for delete using (auth.uid() = created_by_user_id);
