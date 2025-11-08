-- =====================================================
-- 为 cities 表添加英文名称字段
-- 生成时间: 2025-11-05
-- 数据来源: 实际数据库中的 119 个城市
-- 中文城市需要英文名: 119 个
-- 英文城市保持不变: 0 个
-- =====================================================

BEGIN;

-- 添加英文名称字段
ALTER TABLE cities
ADD COLUMN IF NOT EXISTS name_en VARCHAR(100);

-- 添加列注释
COMMENT ON COLUMN cities.name_en IS '城市英文名称';

-- 为中文城市名添加英文翻译
UPDATE cities SET name_en = 'Qitaihe' WHERE name = '七台河市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Shanghai' WHERE name = '上海' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Linfen' WHERE name = '临汾市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Dandong' WHERE name = '丹东市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Lishui' WHERE name = '丽水市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Ulanqab' WHERE name = '乌兰察布市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Wuhai' WHERE name = '乌海市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Bozhou' WHERE name = '亳州市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Yichun' WHERE name = '伊春市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Jiamusi' WHERE name = '佳木斯市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Baoding' WHERE name = '保定' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Lu''an' WHERE name = '六安市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Hinggan League' WHERE name = '兴安盟' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Baotou' WHERE name = '包头市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Beijing' WHERE name = '北京' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Nanjing' WHERE name = '南京' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Nantong' WHERE name = '南通' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Shuangyashan' WHERE name = '双鸭山市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Hefei' WHERE name = '合肥' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Jilin' WHERE name = '吉林市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Lvliang' WHERE name = '吕梁市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Hulunbuir' WHERE name = '呼伦贝尔市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Hohhot' WHERE name = '呼和浩特' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Harbin' WHERE name = '哈尔滨' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Tangshan' WHERE name = '唐山' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Jiaxing' WHERE name = '嘉兴' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Siping' WHERE name = '四平市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Daxing''anling Prefecture' WHERE name = '大兴安岭地区' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Datong' WHERE name = '大同市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Daqing' WHERE name = '大庆市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Dalian' WHERE name = '大连' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Tianjin' WHERE name = '天津' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Taiyuan' WHERE name = '太原' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Ningbo' WHERE name = '宁波' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Anqing' WHERE name = '安庆' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Xuancheng' WHERE name = '宣城市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Suzhou' WHERE name = '宿州市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Suqian' WHERE name = '宿迁' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Bayannur' WHERE name = '巴彦淖尔市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Changzhou' WHERE name = '常州市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Langfang' WHERE name = '廊坊市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Yanbian Korean Autonomous Prefecture' WHERE name = '延边朝鲜族自治州' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Zhangjiakou' WHERE name = '张家口市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Xuzhou' WHERE name = '徐州' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Xinzhou' WHERE name = '忻州市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Yangzhou' WHERE name = '扬州' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Chengde' WHERE name = '承德市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Fushun' WHERE name = '抚顺市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Wuxi' WHERE name = '无锡' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Jinzhong' WHERE name = '晋中市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Jincheng' WHERE name = '晋城市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Shuozhou' WHERE name = '朔州市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Chaoyang' WHERE name = '朝阳市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Benxi' WHERE name = '本溪市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Hangzhou' WHERE name = '杭州' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Songyuan' WHERE name = '松原市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Chizhou' WHERE name = '池州市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Shenyang' WHERE name = '沈阳' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Cangzhou' WHERE name = '沧州市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Taizhou' WHERE name = '泰州' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Taizhou' WHERE name = '泰州市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Huaibei' WHERE name = '淮北市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Huainan' WHERE name = '淮南市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Huai''an' WHERE name = '淮安' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Wenzhou' WHERE name = '温州' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Huzhou' WHERE name = '湖州' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Chuzhou' WHERE name = '滁州市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Mudanjiang' WHERE name = '牡丹江市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Baicheng' WHERE name = '白城市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Baishan' WHERE name = '白山市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Yancheng' WHERE name = '盐城' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Panjin' WHERE name = '盘锦市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Shijiazhuang' WHERE name = '石家庄' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Qinhuangdao' WHERE name = '秦皇岛市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Shaoxing' WHERE name = '绍兴' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Suihua' WHERE name = '绥化市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Zhoushan' WHERE name = '舟山市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Wuhu' WHERE name = '芜湖' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Suzhou' WHERE name = '苏州' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Yingkou' WHERE name = '营口市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Huludao' WHERE name = '葫芦岛市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Bengbu' WHERE name = '蚌埠' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Hengshui' WHERE name = '衡水市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Quzhou' WHERE name = '衢州市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Chifeng' WHERE name = '赤峰市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Liaoyuan' WHERE name = '辽源市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Liaoyang' WHERE name = '辽阳市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Yuncheng' WHERE name = '运城市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Lianyungang' WHERE name = '连云港' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Tonghua' WHERE name = '通化市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Tongliao' WHERE name = '通辽市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Xingtai' WHERE name = '邢台市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Handan' WHERE name = '邯郸市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Ordos' WHERE name = '鄂尔多斯市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Chongqing' WHERE name = '重庆' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Jinhua' WHERE name = '金华' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Tieling' WHERE name = '铁岭市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Tongling' WHERE name = '铜陵市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Xilingol League' WHERE name = '锡林郭勒盟' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Jinzhou' WHERE name = '锦州市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Zhenjiang' WHERE name = '镇江' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Changchun' WHERE name = '长春' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Changzhi' WHERE name = '长治市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Fuxin' WHERE name = '阜新市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Fuyang' WHERE name = '阜阳市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Yangquan' WHERE name = '阳泉市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Alxa League' WHERE name = '阿拉善盟' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Anshan' WHERE name = '鞍山市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Maanshan' WHERE name = '马鞍山' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Jixi' WHERE name = '鸡西市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Hegang' WHERE name = '鹤岗市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Huangshan' WHERE name = '黄山市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Heihe' WHERE name = '黑河市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Qiqihar' WHERE name = '齐齐哈尔市' AND country = 'China' AND name_en IS NULL;
UPDATE cities SET name_en = 'Bali' WHERE name = '巴厘岛' AND country = 'Indonesia' AND name_en IS NULL;
UPDATE cities SET name_en = 'Mexico City' WHERE name = '墨西哥城' AND country = 'Mexico' AND name_en IS NULL;
UPDATE cities SET name_en = 'Lisbon' WHERE name = '里斯本' AND country = 'Portugal' AND name_en IS NULL;
UPDATE cities SET name_en = 'Barcelona' WHERE name = '巴塞罗那' AND country = 'Spain' AND name_en IS NULL;
UPDATE cities SET name_en = 'Chiang Mai' WHERE name = '清迈' AND country = 'Thailand' AND name_en IS NULL;

-- 为已经是英文的城市,将 name_en 设置为相同值
UPDATE cities SET name_en = name WHERE name_en IS NULL AND name ~ '^[a-zA-Z\s\-'']+$';

COMMIT;

-- 创建索引以提高查询性能
CREATE INDEX IF NOT EXISTS idx_cities_name_en ON cities(name_en);

-- 查看更新结果
SELECT name, name_en, country FROM cities ORDER BY country, name LIMIT 50;

ANALYZE cities;