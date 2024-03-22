# XData

This project contains examples of code in C# for working with xData (extended data) AutoCAD objects. This data is created and used in AVC plugins. In particular, xDataMetric stores solid metrics obtained by the SolSize command.
These examples do not represent a ready-made library. They are intended to be copied into other projects, other plugins that need to interact with AVC plugins.
https://sites.google.com/site/avcplugins/xdata

# Important note. 
Do not attempt to run CAD and AVC plugins while Visual Studio or other debuggers are running. Conflicts are possible up to AutoCAD fatalities. Block AVC plugins from loading when you are debugging your plugins.

# RU

В этом проекте собраны примеры кода на C# для работы xData (расширенными данными) объектов AutoCAD. Эти данные создаются и используются в плагинах AVC. В частности в xDataMetric хранятся метрики солидов, полученные командой SolSize.
Данные примеры не представляют собой готовую библиотеку. Они предназначены для копирования в другие проекты, другие плагины, которым нужно взаимодействовать с плагинами AVC.
Подробнее тут https://sites.google.com/site/avcprg/xdata

# Важное примечание. 
Не пытайтесь запускать CAD и плагины AVC, когда работает Visual Studio или другие отладчики. Возможну конфликты вплоть до фаталов AutoCAD. Заблокируйте загрузку плагинов AVC, когда отлаживаете свои плагины.
