# FAQ

У маркетплейсов по типу Ozon, WB, Yandex.Market нет оповещений на конкретные лоты при снижении цены (как на том же Авито). Потому я написал помощника, выполняющего эту функцию.
Работает путем опроса каждые N секунд url-адресов.

# Как установить (Windows)

1. Зайти [вот сюда](https://my.telegram.org/auth) -> API Development tools -> Create New Application (Type=Desktop) -> получить `api_id` и `api_hash`.
2. Скачать [вот этот скрипт](https://github.com/nshadx/pricewatcheruserbot/blob/master/pricewatcheruserbot/launcher-windows.ps1)
3. Запустить Windows PowerShell (Run As Administrator)
4. `cd <путь до папки со скриптом>`
5. `.\launcher-windows.ps1`

# Команды

Бот принимает сообщения и отвечает на них только в разделе `Saved Messages` (т.е. отправка самому себе).

1. `/add <link>`, где `<link>` - валидный url с Ozon, WB, Yandex.Market;
2. `/lst` выводит список url в работе с порядковым номером;
3. `/rem <number>` удаляет ссылку из отслеживаемых, где `<number>` порядковый номер из `/lst`.
