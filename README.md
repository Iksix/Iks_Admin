## Iks_Admin
Админ система с меню <br>
![image](imgs/MenuScreen.png)

## Команды

- `css_admin` - Открыть админ меню | Флаги не нужны
- 
- `css_ban uid/sid duration reason <name if needed>` - Забанить игрока | Флаг: `b`
- `css_banip uid/sid/ip(offline only) duration reason <name if needed>` - Забанить игрока по айпи | Флаг: `b`
- `css_unban sid/ip` - Разбанить игрока | Флаг: `u`
- `css_mute uid/sid duration reason <name if needed>` - Замутить игрока | Флаг: `m`
- `css_unmute uid/sid duration reason <name if needed>` - Размутить игрока | Флаг: `m`
- `css_gag uid/sid duration reason <name if needed>` - Дать гаг игроку | Флаг: `g`
- `css_ungag uid/sid` - Снят гаг с игрока | Флаг: `g`
- 
- `css_adminadd <sid> <name> <flags/-> <immunity> <group_id> <endtime> <server_id>` - Добавить админа | Флаг: `z`
- `group_id` = -1 => Нет группы
- `immunity` = -1 => Иммунитет из группы
- `flags` = "" => Флаги из группы
- `server_id` = "" => Все сервера
- `endtime` - Время до конца админки | `Не функционирует`
- 
- `css_admindel <sid>` - Удалить админа | Флаг: `z`
- `css_reload_admins` - Перезагрузить админов | Флаг: `z`
- `css_reload_admins_cfg` - Перезагрузить конфиг | Флаг: `z`

## Настройка
- Настройте базу данных `cfg`
- Поменяйте ServerId на нужный `cfg`
- Настройте причины `cfg`
- Настройте переводы `lang/en.json`

## Флаги:
- `z` - Все права
- `b` - Бан
- `k` - Кик
- `m` - Мут
- `g` - Гаг
- `u` - Разбан
- `s` - css_slay `В планах...`
- `t` - Смена сторон `В планах...`

## Планы
- Лог в дискорд
- `css_slay` - будет в меню
- `css_switchteam` - будет в меню
- `css_changeteam` - будет в меню




