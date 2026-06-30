-- ============================================================
-- Feature 11 Phase 6: Wishlist Sync Tables
-- Run in Supabase SQL Editor (once per project)
-- ============================================================

-- Enable UUID extension (usually already enabled)
create extension if not exists "pgcrypto";

-- ── wishlists ────────────────────────────────────────────────
create table if not exists public.wishlists (
    id          uuid primary key default gen_random_uuid(),
    user_id     uuid not null references auth.users(id) on delete cascade,
    name        text not null default '',
    description text not null default '',
    created_at  timestamptz not null default now(),
    updated_at  timestamptz not null default now(),
    is_deleted  boolean not null default false
);

alter table public.wishlists enable row level security;

create policy "wishlists: owner full access"
    on public.wishlists for all
    using (auth.uid() = user_id)
    with check (auth.uid() = user_id);

create policy "wishlists: shared read access"
    on public.wishlists for select
    using (
        id in (
            select wishlist_id::uuid
            from public.wishlist_shares
            where shared_with_user_id = auth.uid()
              and is_accepted = true
              and is_deleted = false
        )
    );

-- ── wishlist_columns ─────────────────────────────────────────
create table if not exists public.wishlist_columns (
    id          uuid primary key default gen_random_uuid(),
    wishlist_id uuid not null references public.wishlists(id) on delete cascade,
    name        text not null default '',
    col_type    int  not null default 0,
    col_order   int  not null default 0,
    options_json text,
    is_hidden   boolean not null default false,
    updated_at  timestamptz not null default now(),
    is_deleted  boolean not null default false
);

alter table public.wishlist_columns enable row level security;

create policy "wishlist_columns: owner full access"
    on public.wishlist_columns for all
    using (
        wishlist_id in (select id from public.wishlists where user_id = auth.uid())
    )
    with check (
        wishlist_id in (select id from public.wishlists where user_id = auth.uid())
    );

create policy "wishlist_columns: shared read access"
    on public.wishlist_columns for select
    using (
        wishlist_id in (
            select wishlist_id::uuid
            from public.wishlist_shares
            where shared_with_user_id = auth.uid()
              and is_accepted = true
              and is_deleted = false
        )
    );

-- ── wishlist_rows ────────────────────────────────────────────
create table if not exists public.wishlist_rows (
    id          uuid primary key default gen_random_uuid(),
    wishlist_id uuid not null references public.wishlists(id) on delete cascade,
    row_order   int  not null default 0,
    updated_at  timestamptz not null default now(),
    is_deleted  boolean not null default false
);

alter table public.wishlist_rows enable row level security;

create policy "wishlist_rows: owner full access"
    on public.wishlist_rows for all
    using (
        wishlist_id in (select id from public.wishlists where user_id = auth.uid())
    )
    with check (
        wishlist_id in (select id from public.wishlists where user_id = auth.uid())
    );

create policy "wishlist_rows: shared read access"
    on public.wishlist_rows for select
    using (
        wishlist_id in (
            select wishlist_id::uuid
            from public.wishlist_shares
            where shared_with_user_id = auth.uid()
              and is_accepted = true
              and is_deleted = false
        )
    );

create policy "wishlist_rows: shared edit access"
    on public.wishlist_rows for all
    using (
        wishlist_id in (
            select wishlist_id::uuid
            from public.wishlist_shares
            where shared_with_user_id = auth.uid()
              and permission = 'edit'
              and is_accepted = true
              and is_deleted = false
        )
    )
    with check (
        wishlist_id in (
            select wishlist_id::uuid
            from public.wishlist_shares
            where shared_with_user_id = auth.uid()
              and permission = 'edit'
              and is_accepted = true
              and is_deleted = false
        )
    );

-- ── wishlist_cells ───────────────────────────────────────────
create table if not exists public.wishlist_cells (
    id         uuid primary key default gen_random_uuid(),
    row_id     uuid not null references public.wishlist_rows(id) on delete cascade,
    column_id  uuid not null references public.wishlist_columns(id) on delete cascade,
    value      text,
    extra      text,
    updated_at timestamptz not null default now()
);

alter table public.wishlist_cells enable row level security;

create policy "wishlist_cells: owner full access"
    on public.wishlist_cells for all
    using (
        row_id in (
            select wr.id from public.wishlist_rows wr
            join public.wishlists w on w.id = wr.wishlist_id
            where w.user_id = auth.uid()
        )
    )
    with check (
        row_id in (
            select wr.id from public.wishlist_rows wr
            join public.wishlists w on w.id = wr.wishlist_id
            where w.user_id = auth.uid()
        )
    );

create policy "wishlist_cells: shared read access"
    on public.wishlist_cells for select
    using (
        row_id in (
            select wr.id from public.wishlist_rows wr
            join public.wishlist_shares ws on ws.wishlist_id = wr.wishlist_id::text
            where ws.shared_with_user_id = auth.uid()
              and ws.is_accepted = true
              and ws.is_deleted = false
        )
    );

-- ── wishlist_shares ──────────────────────────────────────────
create table if not exists public.wishlist_shares (
    id                   uuid primary key default gen_random_uuid(),
    wishlist_id          uuid not null references public.wishlists(id) on delete cascade,
    owner_user_id        uuid not null references auth.users(id),
    shared_with_email    text not null,
    shared_with_user_id  uuid references auth.users(id),
    permission           text not null default 'view', -- 'view' | 'edit'
    is_accepted          boolean not null default false,
    created_at           timestamptz not null default now(),
    is_deleted           boolean not null default false
);

alter table public.wishlist_shares enable row level security;

create policy "wishlist_shares: owner manage"
    on public.wishlist_shares for all
    using (owner_user_id = auth.uid())
    with check (owner_user_id = auth.uid());

create policy "wishlist_shares: invited user can read and accept"
    on public.wishlist_shares for select
    using (shared_with_user_id = auth.uid());

create policy "wishlist_shares: invited user can accept"
    on public.wishlist_shares for update
    using (shared_with_user_id = auth.uid())
    with check (shared_with_user_id = auth.uid());

-- ── Indexes ──────────────────────────────────────────────────
create index if not exists idx_wishlists_user_id       on public.wishlists(user_id);
create index if not exists idx_wishlist_columns_wishlist on public.wishlist_columns(wishlist_id);
create index if not exists idx_wishlist_rows_wishlist    on public.wishlist_rows(wishlist_id);
create index if not exists idx_wishlist_cells_row        on public.wishlist_cells(row_id);
create index if not exists idx_wishlist_shares_wishlist  on public.wishlist_shares(wishlist_id);
create index if not exists idx_wishlist_shares_invited   on public.wishlist_shares(shared_with_user_id);
