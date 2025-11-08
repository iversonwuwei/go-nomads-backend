import json
import re

# 读取城市数据
with open(r'e:\Workspaces\WaldenProjects\go-nomads\cities_for_name_en.json', 'r', encoding='utf-8-sig') as f:
    cities = json.load(f)

# 检测是否包含英文字母
def has_english(text):
    return bool(re.search(r'[a-zA-Z]', text))

# 完整的城市名称翻译映射表(中文 -> 英文)
cn_to_en = {
    # 中国主要城市
    "北京": "Beijing",
    "上海": "Shanghai",
    "广州": "Guangzhou",
    "深圳": "Shenzhen",
    "成都": "Chengdu",
    "杭州": "Hangzhou",
    "重庆": "Chongqing",
    "西安": "Xi'an",
    "天津": "Tianjin",
    "南京": "Nanjing",
    "武汉": "Wuhan",
    "苏州": "Suzhou",
    "郑州": "Zhengzhou",
    "长沙": "Changsha",
    "沈阳": "Shenyang",
    "青岛": "Qingdao",
    "大连": "Dalian",
    "厦门": "Xiamen",
    "宁波": "Ningbo",
    "昆明": "Kunming",
    "哈尔滨": "Harbin",
    "济南": "Jinan",
    "福州": "Fuzhou",
    "长春": "Changchun",
    "石家庄": "Shijiazhuang",
    "合肥": "Hefei",
    "南昌": "Nanchang",
    "贵阳": "Guiyang",
    "太原": "Taiyuan",
    "南宁": "Nanning",
    "乌鲁木齐": "Urumqi",
    "兰州": "Lanzhou",
    "海口": "Haikou",
    "银川": "Yinchuan",
    "呼和浩特": "Hohhot",
    "拉萨": "Lhasa",
    "西宁": "Xining",
    
    # 河北省
    "保定": "Baoding",
    "唐山": "Tangshan",
    "秦皇岛": "Qinhuangdao",
    "秦皇岛市": "Qinhuangdao",
    "邯郸": "Handan",
    "邯郸市": "Handan",
    "邢台": "Xingtai",
    "邢台市": "Xingtai",
    "张家口": "Zhangjiakou",
    "张家口市": "Zhangjiakou",
    "承德": "Chengde",
    "承德市": "Chengde",
    "沧州": "Cangzhou",
    "沧州市": "Cangzhou",
    "廊坊": "Langfang",
    "廊坊市": "Langfang",
    "衡水": "Hengshui",
    "衡水市": "Hengshui",
    
    # 山西省
    "大同": "Datong",
    "大同市": "Datong",
    "阳泉": "Yangquan",
    "阳泉市": "Yangquan",
    "长治": "Changzhi",
    "长治市": "Changzhi",
    "晋城": "Jincheng",
    "晋城市": "Jincheng",
    "朔州": "Shuozhou",
    "朔州市": "Shuozhou",
    "晋中": "Jinzhong",
    "晋中市": "Jinzhong",
    "运城": "Yuncheng",
    "运城市": "Yuncheng",
    "忻州": "Xinzhou",
    "忻州市": "Xinzhou",
    "临汾": "Linfen",
    "临汾市": "Linfen",
    "吕梁": "Lvliang",
    "吕梁市": "Lvliang",
    
    # 内蒙古
    "包头": "Baotou",
    "包头市": "Baotou",
    "乌海": "Wuhai",
    "乌海市": "Wuhai",
    "赤峰": "Chifeng",
    "赤峰市": "Chifeng",
    "通辽": "Tongliao",
    "通辽市": "Tongliao",
    "鄂尔多斯": "Ordos",
    "鄂尔多斯市": "Ordos",
    "呼伦贝尔": "Hulunbuir",
    "呼伦贝尔市": "Hulunbuir",
    "巴彦淖尔": "Bayannur",
    "巴彦淖尔市": "Bayannur",
    "乌兰察布": "Ulanqab",
    "乌兰察布市": "Ulanqab",
    "兴安盟": "Hinggan League",
    "锡林郭勒盟": "Xilingol League",
    "阿拉善盟": "Alxa League",
    
    # 辽宁省
    "鞍山": "Anshan",
    "鞍山市": "Anshan",
    "抚顺": "Fushun",
    "抚顺市": "Fushun",
    "本溪": "Benxi",
    "本溪市": "Benxi",
    "丹东": "Dandong",
    "丹东市": "Dandong",
    "锦州": "Jinzhou",
    "锦州市": "Jinzhou",
    "营口": "Yingkou",
    "营口市": "Yingkou",
    "阜新": "Fuxin",
    "阜新市": "Fuxin",
    "辽阳": "Liaoyang",
    "辽阳市": "Liaoyang",
    "盘锦": "Panjin",
    "盘锦市": "Panjin",
    "铁岭": "Tieling",
    "铁岭市": "Tieling",
    "朝阳": "Chaoyang",
    "朝阳市": "Chaoyang",
    "葫芦岛": "Huludao",
    "葫芦岛市": "Huludao",
    
    # 吉林省
    "吉林": "Jilin",
    "吉林市": "Jilin",
    "四平": "Siping",
    "四平市": "Siping",
    "辽源": "Liaoyuan",
    "辽源市": "Liaoyuan",
    "通化": "Tonghua",
    "通化市": "Tonghua",
    "白山": "Baishan",
    "白山市": "Baishan",
    "松原": "Songyuan",
    "松原市": "Songyuan",
    "白城": "Baicheng",
    "白城市": "Baicheng",
    "延边朝鲜族自治州": "Yanbian Korean Autonomous Prefecture",
    
    # 黑龙江省
    "齐齐哈尔": "Qiqihar",
    "齐齐哈尔市": "Qiqihar",
    "鸡西": "Jixi",
    "鸡西市": "Jixi",
    "鹤岗": "Hegang",
    "鹤岗市": "Hegang",
    "双鸭山": "Shuangyashan",
    "双鸭山市": "Shuangyashan",
    "大庆": "Daqing",
    "大庆市": "Daqing",
    "伊春": "Yichun",
    "伊春市": "Yichun",
    "佳木斯": "Jiamusi",
    "佳木斯市": "Jiamusi",
    "七台河": "Qitaihe",
    "七台河市": "Qitaihe",
    "牡丹江": "Mudanjiang",
    "牡丹江市": "Mudanjiang",
    "黑河": "Heihe",
    "黑河市": "Heihe",
    "绥化": "Suihua",
    "绥化市": "Suihua",
    "大兴安岭地区": "Daxing'anling Prefecture",
    
    # 江苏省
    "常州市": "Changzhou",
    "无锡": "Wuxi",
    "常州": "Changzhou",
    "南通": "Nantong",
    "扬州": "Yangzhou",
    "徐州": "Xuzhou",
    "连云港": "Lianyungang",
    "淮安": "Huai'an",
    "盐城": "Yancheng",
    "镇江": "Zhenjiang",
    "泰州": "Taizhou",
    "泰州市": "Taizhou",
    "宿迁": "Suqian",
    
    # 浙江省
    "温州": "Wenzhou",
    "绍兴": "Shaoxing",
    "金华": "Jinhua",
    "台州": "Taizhou",
    "湖州": "Huzhou",
    "嘉兴": "Jiaxing",
    "衢州": "Quzhou",
    "衢州市": "Quzhou",
    "舟山": "Zhoushan",
    "舟山市": "Zhoushan",
    "丽水": "Lishui",
    "丽水市": "Lishui",
    
    # 安徽省
    "芜湖": "Wuhu",
    "蚌埠": "Bengbu",
    "安庆": "Anqing",
    "马鞍山": "Maanshan",
    "淮南": "Huainan",
    "淮南市": "Huainan",
    "淮北": "Huaibei",
    "淮北市": "Huaibei",
    "铜陵": "Tongling",
    "铜陵市": "Tongling",
    "黄山": "Huangshan",
    "黄山市": "Huangshan",
    "滁州": "Chuzhou",
    "滁州市": "Chuzhou",
    "阜阳": "Fuyang",
    "阜阳市": "Fuyang",
    "宿州": "Suzhou",
    "宿州市": "Suzhou",
    "六安": "Lu'an",
    "六安市": "Lu'an",
    "亳州": "Bozhou",
    "亳州市": "Bozhou",
    "池州": "Chizhou",
    "池州市": "Chizhou",
    "宣城": "Xuancheng",
    "宣城市": "Xuancheng",
    
    # 广东省
    "东莞": "Dongguan",
    "佛山": "Foshan",
    "珠海": "Zhuhai",
    "惠州": "Huizhou",
    "中山": "Zhongshan",
    "江门": "Jiangmen",
    
    # 泰国城市
    "曼谷": "Bangkok",
    "清迈": "Chiang Mai",
    "普吉": "Phuket",
    "芭提雅": "Pattaya",
    "春武里": "Chon Buri",
    "合艾": "Hat Yai",
    "呵叻": "Nakhon Ratchasima",
    "乌隆": "Udon Thani",
    "孔敬": "Khon Kaen",
    "素叻他尼": "Surat Thani",
    
    # 其他国际城市
    "东京": "Tokyo",
    "大阪": "Osaka",
    "新加坡": "Singapore",
    "巴厘岛": "Bali",
    "巴塞罗那": "Barcelona",
    "里斯本": "Lisbon",
    "墨西哥城": "Mexico City",
}

# 英文 -> 中文映射表
en_to_cn = {v: k for k, v in cn_to_en.items()}

# 分析城市数据
chinese_cities = []  # 需要添加英文名的中文城市
english_cities = []  # 需要添加中文名的英文城市
unknown_cities = []  # 无法识别的城市

for city in cities:
    name = city['name']
    country = city['country']
    
    if has_english(name):
        # 英文城市名
        if name in en_to_cn:
            english_cities.append({
                'name': name,
                'name_cn': en_to_cn[name],
                'country': country
            })
        else:
            # 已经是英文,直接使用
            english_cities.append({
                'name': name,
                'name_cn': None,  # 不需要中文翻译
                'country': country
            })
    else:
        # 中文城市名
        if name in cn_to_en:
            chinese_cities.append({
                'name': name,
                'name_en': cn_to_en[name],
                'country': country
            })
        else:
            unknown_cities.append({
                'name': name,
                'country': country
            })

# 生成 SQL 脚本
sql_lines = [
    "-- =====================================================",
    "-- 为 cities 表添加英文名称字段",
    "-- 生成时间: 2025-11-05",
    f"-- 数据来源: 实际数据库中的 {len(cities)} 个城市",
    f"-- 中文城市需要英文名: {len(chinese_cities)} 个",
    f"-- 英文城市保持不变: {len(english_cities)} 个",
    "-- =====================================================",
    "",
    "BEGIN;",
    "",
    "-- 添加英文名称字段",
    "ALTER TABLE cities",
    "ADD COLUMN IF NOT EXISTS name_en VARCHAR(100);",
    "",
    "-- 添加列注释",
    "COMMENT ON COLUMN cities.name_en IS '城市英文名称';",
    "",
]

# 添加中文城市的英文名
if chinese_cities:
    sql_lines.append("-- 为中文城市名添加英文翻译")
    for city in sorted(chinese_cities, key=lambda x: (x['country'], x['name'])):
        name = city['name'].replace("'", "''")
        name_en = city['name_en'].replace("'", "''")
        country = city['country'].replace("'", "''")
        sql_lines.append(f"UPDATE cities SET name_en = '{name_en}' WHERE name = '{name}' AND country = '{country}' AND name_en IS NULL;")
    sql_lines.append("")

# 为英文城市设置 name_en = name
sql_lines.append("-- 为已经是英文的城市,将 name_en 设置为相同值")
sql_lines.append("UPDATE cities SET name_en = name WHERE name_en IS NULL AND name ~ '^[a-zA-Z\\s\\-'']+$';")
sql_lines.append("")

sql_lines.append("COMMIT;")
sql_lines.append("")
sql_lines.append("-- 创建索引以提高查询性能")
sql_lines.append("CREATE INDEX IF NOT EXISTS idx_cities_name_en ON cities(name_en);")
sql_lines.append("")
sql_lines.append("-- 查看更新结果")
sql_lines.append("SELECT name, name_en, country FROM cities ORDER BY country, name LIMIT 50;")
sql_lines.append("")
sql_lines.append("ANALYZE cities;")

if unknown_cities:
    sql_lines.append("")
    sql_lines.append("-- =====================================================")
    sql_lines.append("-- 以下城市无法自动翻译(可能需要手动添加):")
    sql_lines.append("-- =====================================================")
    for city in unknown_cities:
        sql_lines.append(f"-- {city['name']} ({city['country']})")

# 写入 SQL 文件
sql_content = '\n'.join(sql_lines)
with open(r'e:\Workspaces\WaldenProjects\go-nomads\database\migrations\add_name_en_to_cities.sql', 'w', encoding='utf-8') as f:
    f.write(sql_content)

print("✅ SQL 脚本已生成!")
print("\n📊 统计信息:")
print(f"   - 数据库总城市数: {len(cities)}")
print(f"   - 中文城市(需要英文名): {len(chinese_cities)}")
print(f"   - 英文城市(保持原样): {len(english_cities)}")
print(f"   - 无法识别的城市: {len(unknown_cities)}")

print("\n📄 文件位置:")
print("   e:\\Workspaces\\WaldenProjects\\go-nomads\\database\\migrations\\add_name_en_to_cities.sql")

if chinese_cities:
    print("\n✅ 已添加英文翻译的城市 (前 10 个):")
    for city in chinese_cities[:10]:
        print(f"   {city['name']} -> {city['name_en']} ({city['country']})")
    if len(chinese_cities) > 10:
        print(f"   ... 还有 {len(chinese_cities) - 10} 个")

if unknown_cities:
    print("\n⚠️  以下城市无法自动翻译:")
    for city in unknown_cities:
        print(f"   - {city['name']} ({city['country']})")
