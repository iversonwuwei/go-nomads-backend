-- =====================================================
-- 地理数据完整初始化脚本 (简化版)
-- 包含：40个国家 + 34个省份 + 主要城市
-- 执行方式：在 Supabase SQL Editor 中运行
-- =====================================================

-- 步骤 1: 插入国家数据
-- =====================================================
INSERT INTO countries (id, name, name_zh, code, code_alpha3, continent, calling_code, is_active, created_at)
VALUES 
  (gen_random_uuid(), 'China', '中国', 'CN', 'CHN', 'Asia', '+86', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'United States', '美国', 'US', 'USA', 'North America', '+1', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Japan', '日本', 'JP', 'JPN', 'Asia', '+81', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Germany', '德国', 'DE', 'DEU', 'Europe', '+49', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'United Kingdom', '英国', 'GB', 'GBR', 'Europe', '+44', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'France', '法国', 'FR', 'FRA', 'Europe', '+33', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Italy', '意大利', 'IT', 'ITA', 'Europe', '+39', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Canada', '加拿大', 'CA', 'CAN', 'North America', '+1', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'South Korea', '韩国', 'KR', 'KOR', 'Asia', '+82', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Spain', '西班牙', 'ES', 'ESP', 'Europe', '+34', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Australia', '澳大利亚', 'AU', 'AUS', 'Oceania', '+61', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Thailand', '泰国', 'TH', 'THA', 'Asia', '+66', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Singapore', '新加坡', 'SG', 'SGP', 'Asia', '+65', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Malaysia', '马来西亚', 'MY', 'MYS', 'Asia', '+60', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Vietnam', '越南', 'VN', 'VNM', 'Asia', '+84', true, CURRENT_TIMESTAMP)
ON CONFLICT (code) DO NOTHING;

SELECT '✅ 国家数据插入完成' as status, COUNT(*) as count FROM countries;

-- 步骤 2: 插入中国所有省份
-- =====================================================
WITH china AS (SELECT id FROM countries WHERE code = 'CN')
INSERT INTO provinces (id, name, country_id, is_active, created_at)
SELECT 
  gen_random_uuid(), 
  province_name, 
  (SELECT id FROM china),
  true, 
  CURRENT_TIMESTAMP
FROM (VALUES
  -- 直辖市
  ('北京市'), ('天津市'), ('上海市'), ('重庆市'),
  -- 省份
  ('河北省'), ('山西省'), ('辽宁省'), ('吉林省'), ('黑龙江省'),
  ('江苏省'), ('浙江省'), ('安徽省'), ('福建省'), ('江西省'),
  ('山东省'), ('河南省'), ('湖北省'), ('湖南省'), ('广东省'),
  ('海南省'), ('四川省'), ('贵州省'), ('云南省'), ('陕西省'),
  ('甘肃省'), ('青海省'), ('台湾省'),
  -- 自治区
  ('内蒙古自治区'), ('广西壮族自治区'), ('西藏自治区'), 
  ('宁夏回族自治区'), ('新疆维吾尔自治区'),
  -- 特别行政区
  ('香港特别行政区'), ('澳门特别行政区')
) AS provinces(province_name)
ON CONFLICT (country_id, name) DO NOTHING;

SELECT '✅ 省份数据插入完成' as status, COUNT(*) as count FROM provinces;

-- 步骤 3: 批量插入中国主要城市
-- =====================================================
WITH china AS (SELECT id FROM countries WHERE code = 'CN'),
city_data AS (
  SELECT 
    province_name,
    unnest(cities) as city_name
  FROM (VALUES
    ('北京市', ARRAY['北京市']),
    ('天津市', ARRAY['天津市']),
    ('上海市', ARRAY['上海市']),
    ('重庆市', ARRAY['重庆市']),
    ('河北省', ARRAY['石家庄市', '唐山市', '秦皇岛市', '邯郸市', '邢台市', '保定市', '张家口市', '承德市', '沧州市', '廊坊市', '衡水市']),
    ('山西省', ARRAY['太原市', '大同市', '阳泉市', '长治市', '晋城市', '朔州市', '晋中市', '运城市', '忻州市', '临汾市', '吕梁市']),
    ('内蒙古自治区', ARRAY['呼和浩特市', '包头市', '乌海市', '赤峰市', '通辽市', '鄂尔多斯市', '呼伦贝尔市', '巴彦淖尔市', '乌兰察布市', '兴安盟', '锡林郭勒盟', '阿拉善盟']),
    ('辽宁省', ARRAY['沈阳市', '大连市', '鞍山市', '抚顺市', '本溪市', '丹东市', '锦州市', '营口市', '阜新市', '辽阳市', '盘锦市', '铁岭市', '朝阳市', '葫芦岛市']),
    ('吉林省', ARRAY['长春市', '吉林市', '四平市', '辽源市', '通化市', '白山市', '松原市', '白城市', '延边朝鲜族自治州']),
    ('黑龙江省', ARRAY['哈尔滨市', '齐齐哈尔市', '鸡西市', '鹤岗市', '双鸭山市', '大庆市', '伊春市', '佳木斯市', '七台河市', '牡丹江市', '黑河市', '绥化市', '大兴安岭地区']),
    ('江苏省', ARRAY['南京市', '无锡市', '徐州市', '常州市', '苏州市', '南通市', '连云港市', '淮安市', '盐城市', '扬州市', '镇江市', '泰州市', '宿迁市']),
    ('浙江省', ARRAY['杭州市', '宁波市', '温州市', '嘉兴市', '湖州市', '绍兴市', '金华市', '衢州市', '舟山市', '台州市', '丽水市']),
    ('安徽省', ARRAY['合肥市', '芜湖市', '蚌埠市', '淮南市', '马鞍山市', '淮北市', '铜陵市', '安庆市', '黄山市', '滁州市', '阜阳市', '宿州市', '六安市', '亳州市', '池州市', '宣城市']),
    ('福建省', ARRAY['福州市', '厦门市', '莆田市', '三明市', '泉州市', '漳州市', '南平市', '龙岩市', '宁德市']),
    ('江西省', ARRAY['南昌市', '景德镇市', '萍乡市', '九江市', '新余市', '鹰潭市', '赣州市', '吉安市', '宜春市', '抚州市', '上饶市']),
    ('山东省', ARRAY['济南市', '青岛市', '淄博市', '枣庄市', '东营市', '烟台市', '潍坊市', '济宁市', '泰安市', '威海市', '日照市', '临沂市', '德州市', '聊城市', '滨州市', '菏泽市']),
    ('河南省', ARRAY['郑州市', '开封市', '洛阳市', '平顶山市', '安阳市', '鹤壁市', '新乡市', '焦作市', '濮阳市', '许昌市', '漯河市', '三门峡市', '南阳市', '商丘市', '信阳市', '周口市', '驻马店市', '济源市']),
    ('湖北省', ARRAY['武汉市', '黄石市', '十堰市', '宜昌市', '襄阳市', '鄂州市', '荆门市', '孝感市', '荆州市', '黄冈市', '咸宁市', '随州市', '恩施土家族苗族自治州']),
    ('湖南省', ARRAY['长沙市', '株洲市', '湘潭市', '衡阳市', '邵阳市', '岳阳市', '常德市', '张家界市', '益阳市', '郴州市', '永州市', '怀化市', '娄底市', '湘西土家族苗族自治州']),
    ('广东省', ARRAY['广州市', '韶关市', '深圳市', '珠海市', '汕头市', '佛山市', '江门市', '湛江市', '茂名市', '肇庆市', '惠州市', '梅州市', '汕尾市', '河源市', '阳江市', '清远市', '东莞市', '中山市', '潮州市', '揭阳市', '云浮市']),
    ('广西壮族自治区', ARRAY['南宁市', '柳州市', '桂林市', '梧州市', '北海市', '防城港市', '钦州市', '贵港市', '玉林市', '百色市', '贺州市', '河池市', '来宾市', '崇左市']),
    ('海南省', ARRAY['海口市', '三亚市', '三沙市', '儋州市']),
    ('四川省', ARRAY['成都市', '自贡市', '攀枝花市', '泸州市', '德阳市', '绵阳市', '广元市', '遂宁市', '内江市', '乐山市', '南充市', '眉山市', '宜宾市', '广安市', '达州市', '雅安市', '巴中市', '资阳市']),
    ('贵州省', ARRAY['贵阳市', '六盘水市', '遵义市', '安顺市', '毕节市', '铜仁市']),
    ('云南省', ARRAY['昆明市', '曲靖市', '玉溪市', '保山市', '昭通市', '丽江市', '普洱市', '临沧市']),
    ('西藏自治区', ARRAY['拉萨市', '日喀则市', '昌都市', '林芝市', '山南市', '那曲市', '阿里地区']),
    ('陕西省', ARRAY['西安市', '铜川市', '宝鸡市', '咸阳市', '渭南市', '延安市', '汉中市', '榆林市', '安康市', '商洛市']),
    ('甘肃省', ARRAY['兰州市', '嘉峪关市', '金昌市', '白银市', '天水市', '武威市', '张掖市', '平凉市', '酒泉市', '庆阳市', '定西市', '陇南市']),
    ('青海省', ARRAY['西宁市', '海东市']),
    ('宁夏回族自治区', ARRAY['银川市', '石嘴山市', '吴忠市', '固原市', '中卫市']),
    ('新疆维吾尔自治区', ARRAY['乌鲁木齐市', '克拉玛依市', '吐鲁番市', '哈密市']),
    ('台湾省', ARRAY['台北市', '高雄市', '台中市', '台南市']),
    ('香港特别行政区', ARRAY['香港岛', '九龙', '新界']),
    ('澳门特别行政区', ARRAY['澳门半岛', '氹仔', '路环'])
  ) AS province_cities(province_name, cities)
)
INSERT INTO cities (id, name, country, country_id, province_id, region, is_active, created_at)
SELECT 
  gen_random_uuid(),
  cd.city_name,
  'China',
  (SELECT id FROM china),
  p.id,
  p.name,
  true,
  CURRENT_TIMESTAMP
FROM city_data cd
JOIN provinces p ON p.name = cd.province_name AND p.country_id = (SELECT id FROM china);

SELECT '✅ 城市数据插入完成' as status, COUNT(*) as count FROM cities WHERE country_id = (SELECT id FROM countries WHERE code = 'CN');

-- 步骤 4: 数据验证
-- =====================================================
SELECT 
  '📊 数据统计' as section,
  (SELECT COUNT(*) FROM countries) as countries,
  (SELECT COUNT(*) FROM provinces) as provinces,
  (SELECT COUNT(*) FROM cities WHERE country_id = (SELECT id FROM countries WHERE code = 'CN')) as cities;

-- 查看省份和对应的城市数量
SELECT 
  p.name as province,
  COUNT(c.id) as city_count
FROM provinces p
LEFT JOIN cities c ON c.province_id = p.id
WHERE p.country_id = (SELECT id FROM countries WHERE code = 'CN')
GROUP BY p.name
ORDER BY city_count DESC;

SELECT '🎉 地理数据初始化完成!' as message;
