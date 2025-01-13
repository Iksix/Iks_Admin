create table if not exists iks_servers(
    id int not null unique,
    ip varchar(32) not null comment 'ip:port',
    name varchar(64) not null,
    rcon varchar(128) default null,
    created_at int not null,
    updated_at int not null,
    deleted_at int default null
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE utf8mb4_unicode_ci;
create table if not exists iks_groups(
    id int not null auto_increment primary key,
    name varchar(64) not null unique,
    flags varchar(32) not null,
    immunity int not null,
    comment varchar(255) default null
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE utf8mb4_unicode_ci;
create table if not exists iks_admins(
    id int not null auto_increment primary key,
    steam_id varchar(17) not null,
    name varchar(64) not null,
    flags varchar(32) default null,
    immunity int default null,
    group_id int default null,
    discord varchar(64) default null,
    vk varchar(64) default null,
    is_disabled int(1) not null default 0,
    end_at int null,
    created_at int not null,
    updated_at int not null,
    deleted_at int default null,
    foreign key (group_id) references iks_groups(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE utf8mb4_unicode_ci;

insert into iks_admins(steam_id, name, flags, immunity, created_at, updated_at)
select 'CONSOLE', 'CONSOLE', null, 0, unix_timestamp(), unix_timestamp()
where not exists (select 1 from iks_admins where steam_id = 'CONSOLE');

create table if not exists iks_admin_to_server(
    id int not null auto_increment primary key,
    admin_id int not null,
    server_id int not null,
    foreign key (admin_id) references iks_admins(id),
    foreign key (server_id) references iks_servers(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE utf8mb4_unicode_ci;

create table if not exists iks_comms(
    id int not null auto_increment primary key,
    steam_id bigint not null,
    ip varchar(32),
    name varchar(128),
    mute_type int not null comment '0 - voice(mute), 1 - chat(gag), 2 - both(silence)', 
    duration int not null,
    reason varchar(128) not null,
    server_id int default null,
    admin_id int not null,
    unbanned_by int default null,
    unban_reason varchar(128) default null,
    created_at int not null,
    end_at int not null,
    updated_at int not null,
    deleted_at int default null,
    foreign key (admin_id) references iks_admins(id),
    foreign key (unbanned_by) references iks_admins(id),
    foreign key (server_id) references iks_servers(id),
    index `idx_steam_id` (`steam_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE utf8mb4_unicode_ci;

create table if not exists iks_bans(
    id int not null auto_increment primary key,
    steam_id bigint,
    ip varchar(32),
    name varchar(128),
    duration int not null,
    reason varchar(128) not null,
    ban_type tinyint not null default 0 comment '0 - SteamId, 1 - Ip, 2 - Both',
    server_id int default null,
    admin_id int not null,
    unbanned_by int default null,
    unban_reason varchar(128) default null,
    created_at int not null,
    end_at int not null,
    updated_at int not null,
    deleted_at int default null,
    foreign key (admin_id) references iks_admins(id),
    foreign key (unbanned_by) references iks_admins(id),
    foreign key (server_id) references iks_servers(id),
    index `idx_steam_id` (`steam_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE utf8mb4_unicode_ci;
create table if not exists iks_admins_warns(
    id int not null auto_increment primary key,
    admin_id int not null,
    target_id int not null,
    duration int not null,
    reason varchar(128) not null,
    created_at int not null,
    end_at int not null,
    updated_at int not null,
    deleted_at int default null,
    deleted_by int default null,
    foreign key (admin_id) references iks_admins(id),
    foreign key (target_id) references iks_admins(id),
    foreign key (deleted_by) references iks_admins(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE utf8mb4_unicode_ci;
create table if not exists iks_groups_limitations( 
    id int not null auto_increment primary key,
    group_id int not null,
    limitation_key varchar(64) not null,
    limitation_value varchar(32) not null,
    foreign key (group_id) references iks_groups(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE utf8mb4_unicode_ci;

