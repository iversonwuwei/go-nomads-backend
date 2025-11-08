-- =====================================================
-- 更新 cities 表中的城市名称从英文改为中文
-- 生成时间: 2025-11-05
-- 数据来源: 从实际数据库 API 获取的 119 个城市
-- 总共需要更新: 42 个城市(所有英文城市名)
-- =====================================================

BEGIN;

UPDATE cities SET name = '北京' WHERE name = 'Beijing' AND country = 'China';
UPDATE cities SET name = '上海' WHERE name = 'Shanghai' AND country = 'China';
UPDATE cities SET name = '重庆' WHERE name = 'Chongqing' AND country = 'China';
UPDATE cities SET name = '天津' WHERE name = 'Tianjin' AND country = 'China';
UPDATE cities SET name = '太原' WHERE name = 'Taiyuan' AND country = 'China';
UPDATE cities SET name = '石家庄' WHERE name = 'Shijiazhuang' AND country = 'China';
UPDATE cities SET name = '呼和浩特' WHERE name = 'Hohhot' AND country = 'China';
UPDATE cities SET name = '保定' WHERE name = 'Baoding' AND country = 'China';
UPDATE cities SET name = '唐山' WHERE name = 'Tangshan' AND country = 'China';
UPDATE cities SET name = '苏州' WHERE name = 'Suzhou' AND country = 'China';
UPDATE cities SET name = '杭州' WHERE name = 'Hangzhou' AND country = 'China';
UPDATE cities SET name = '南京' WHERE name = 'Nanjing' AND country = 'China';
UPDATE cities SET name = '大连' WHERE name = 'Dalian' AND country = 'China';
UPDATE cities SET name = '沈阳' WHERE name = 'Shenyang' AND country = 'China';
UPDATE cities SET name = '宁波' WHERE name = 'Ningbo' AND country = 'China';
UPDATE cities SET name = '哈尔滨' WHERE name = 'Harbin' AND country = 'China';
UPDATE cities SET name = '合肥' WHERE name = 'Hefei' AND country = 'China';
UPDATE cities SET name = '无锡' WHERE name = 'Wuxi' AND country = 'China';
UPDATE cities SET name = '长春' WHERE name = 'Changchun' AND country = 'China';
UPDATE cities SET name = '南通' WHERE name = 'Nantong' AND country = 'China';
UPDATE cities SET name = '温州' WHERE name = 'Wenzhou' AND country = 'China';
UPDATE cities SET name = '绍兴' WHERE name = 'Shaoxing' AND country = 'China';
UPDATE cities SET name = '嘉兴' WHERE name = 'Jiaxing' AND country = 'China';
UPDATE cities SET name = '湖州' WHERE name = 'Huzhou' AND country = 'China';
UPDATE cities SET name = '泰州' WHERE name = 'Taizhou' AND country = 'China';
UPDATE cities SET name = '金华' WHERE name = 'Jinhua' AND country = 'China';
UPDATE cities SET name = '扬州' WHERE name = 'Yangzhou' AND country = 'China';
UPDATE cities SET name = '镇江' WHERE name = 'Zhenjiang' AND country = 'China';
UPDATE cities SET name = '徐州' WHERE name = 'Xuzhou' AND country = 'China';
UPDATE cities SET name = '盐城' WHERE name = 'Yancheng' AND country = 'China';
UPDATE cities SET name = '连云港' WHERE name = 'Lianyungang' AND country = 'China';
UPDATE cities SET name = '淮安' WHERE name = 'Huai''an' AND country = 'China';
UPDATE cities SET name = '宿迁' WHERE name = 'Suqian' AND country = 'China';
UPDATE cities SET name = '清迈' WHERE name = 'Chiang Mai' AND country = 'Thailand';
UPDATE cities SET name = '巴厘岛' WHERE name = 'Bali' AND country = 'Indonesia';
UPDATE cities SET name = '巴塞罗那' WHERE name = 'Barcelona' AND country = 'Spain';
UPDATE cities SET name = '芜湖' WHERE name = 'Wuhu' AND country = 'China';
UPDATE cities SET name = '蚌埠' WHERE name = 'Bengbu' AND country = 'China';
UPDATE cities SET name = '安庆' WHERE name = 'Anqing' AND country = 'China';
UPDATE cities SET name = '马鞍山' WHERE name = 'Maanshan' AND country = 'China';
UPDATE cities SET name = '里斯本' WHERE name = 'Lisbon' AND country = 'Portugal';
UPDATE cities SET name = '墨西哥城' WHERE name = 'Mexico City' AND country = 'Mexico';

COMMIT;

-- 成功生成 42 条更新语句(全部城市已翻译)

-- 查看更新结果
SELECT name, country FROM cities WHERE name ~ '[a-zA-Z]' ORDER BY country, name;