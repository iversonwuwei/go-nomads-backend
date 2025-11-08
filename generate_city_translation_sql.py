import json
import re

# 读取城市数据
with open(r'e:\Workspaces\WaldenProjects\go-nomads\cities_current.json', 'r', encoding='utf-8-sig') as f:
    cities = json.load(f)

# 检测是否包含英文字母(不包括特殊字符)
def has_english(text):
    return bool(re.search(r'[a-zA-Z]', text))

# 城市名称翻译映射表
city_translations = {
    # 中国主要城市
    "Beijing": "北京",
    "Shanghai": "上海",
    "Guangzhou": "广州",
    "Shenzhen": "深圳",
    "Chengdu": "成都",
    "Hangzhou": "杭州",
    "Chongqing": "重庆",
    "Xi'an": "西安",
    "Xian": "西安",
    "Tianjin": "天津",
    "Nanjing": "南京",
    "Wuhan": "武汉",
    "Suzhou": "苏州",
    "Zhengzhou": "郑州",
    "Changsha": "长沙",
    "Shenyang": "沈阳",
    "Qingdao": "青岛",
    "Dalian": "大连",
    "Xiamen": "厦门",
    "Ningbo": "宁波",
    "Kunming": "昆明",
    "Harbin": "哈尔滨",
    "Jinan": "济南",
    "Fuzhou": "福州",
    "Changchun": "长春",
    "Shijiazhuang": "石家庄",
    "Hefei": "合肥",
    "Nanchang": "南昌",
    "Guiyang": "贵阳",
    "Taiyuan": "太原",
    "Nanning": "南宁",
    "Urumqi": "乌鲁木齐",
    "Lanzhou": "兰州",
    "Haikou": "海口",
    "Yinchuan": "银川",
    "Hohhot": "呼和浩特",
    "Lhasa": "拉萨",
    "Xining": "西宁",
    "Baoding": "保定",
    "Tangshan": "唐山",
    "Dongguan": "东莞",
    "Foshan": "佛山",
    "Zhuhai": "珠海",
    "Huizhou": "惠州",
    "Zhongshan": "中山",
    "Jiangmen": "江门",
    "Shaoxing": "绍兴",
    "Wenzhou": "温州",
    "Jinhua": "金华",
    "Taizhou": "台州",
    "Huzhou": "湖州",
    "Jiaxing": "嘉兴",
    "Wuxi": "无锡",
    "Changzhou": "常州",
    "Nantong": "南通",
    "Yangzhou": "扬州",
    "Xuzhou": "徐州",
    "Lianyungang": "连云港",
    "Huai'an": "淮安",
    "Yancheng": "盐城",
    "Zhenjiang": "镇江",
    "Taizhou": "泰州",
    "Suqian": "宿迁",
    
    # 泰国城市
    "Bangkok": "曼谷",
    "Chiang Mai": "清迈",
    "Phuket": "普吉",
    "Pattaya": "芭提雅",
    "Chon Buri": "春武里",
    "Hat Yai": "合艾",
    "Nakhon Ratchasima": "呵叻",
    "Udon Thani": "乌隆",
    "Khon Kaen": "孔敬",
    "Surat Thani": "素叻他尼",
    "Nonthaburi": "暖武里",
    "Pak Kret": "北榄",
    "Samut Prakan": "北榄府",
    "Ubon Ratchathani": "乌汶",
    "Nakhon Si Thammarat": "洛坤",
    "Chiang Rai": "清莱",
    "Songkhla": "宋卡",
    "Nakhon Sawan": "那空沙旺",
    "Rayong": "罗勇",
    "Lampang": "南邦",
    
    # 日本城市
    "Tokyo": "东京",
    "Osaka": "大阪",
    "Kyoto": "京都",
    "Yokohama": "横滨",
    "Nagoya": "名古屋",
    "Sapporo": "札幌",
    "Fukuoka": "福冈",
    "Kobe": "神户",
    
    # 韩国城市
    "Seoul": "首尔",
    "Busan": "釜山",
    "Incheon": "仁川",
    "Daegu": "大邱",
    "Daejeon": "大田",
    "Gwangju": "光州",
    "Jeju": "济州",
    
    # 其他亚洲城市
    "Singapore": "新加坡",
    "Kuala Lumpur": "吉隆坡",
    "Penang": "槟城",
    "Johor Bahru": "新山",
    "Hanoi": "河内",
    "Ho Chi Minh City": "胡志明市",
    "Da Nang": "岘港",
    "Jakarta": "雅加达",
    "Bali": "巴厘岛",
    "Surabaya": "泗水",
    "Manila": "马尼拉",
    "Cebu": "宿务",
    "Mumbai": "孟买",
    "New Delhi": "新德里",
    "Bangalore": "班加罗尔",
    "Kolkata": "加尔各答",
    "Chennai": "金奈",
    
    # 欧洲城市
    "London": "伦敦",
    "Paris": "巴黎",
    "Berlin": "柏林",
    "Rome": "罗马",
    "Madrid": "马德里",
    "Barcelona": "巴塞罗那",
    "Amsterdam": "阿姆斯特丹",
    "Brussels": "布鲁塞尔",
    "Vienna": "维也纳",
    "Zurich": "苏黎世",
    "Moscow": "莫斯科",
    "Saint Petersburg": "圣彼得堡",
    
    # 美洲城市
    "New York": "纽约",
    "Los Angeles": "洛杉矶",
    "Chicago": "芝加哥",
    "San Francisco": "旧金山",
    "Seattle": "西雅图",
    "Boston": "波士顿",
    "Washington": "华盛顿",
    "Miami": "迈阿密",
    "Las Vegas": "拉斯维加斯",
    "Toronto": "多伦多",
    "Vancouver": "温哥华",
    "Montreal": "蒙特利尔",
    
    # 澳洲城市
    "Sydney": "悉尼",
    "Melbourne": "墨尔本",
    "Brisbane": "布里斯班",
    "Perth": "珀斯",
    
    # 中东城市
    "Dubai": "迪拜",
    "Abu Dhabi": "阿布扎比",
    "Tel Aviv": "特拉维夫",
}

# 分析需要更新的城市
cities_to_update = []
for city in cities:
    name = city['name']
    country = city['country']
    
    if has_english(name):
        # 如果城市名包含英文
        if name in city_translations:
            cities_to_update.append({
                'old_name': name,
                'new_name': city_translations[name],
                'country': country
            })
        else:
            # 如果没有翻译,记录下来
            cities_to_update.append({
                'old_name': name,
                'new_name': None,  # 需要手动添加翻译
                'country': country
            })

# 生成 SQL 脚本
sql_lines = [
    "-- =====================================================",
    "-- 更新 cities 表中的城市名称从英文改为中文",
    "-- 生成时间: 2025-11-05",
    f"-- 总共需要更新: {len([c for c in cities_to_update if c['new_name']])} 个城市",
    f"-- 缺少翻译: {len([c for c in cities_to_update if not c['new_name']])} 个城市",
    "-- =====================================================",
    "",
    "BEGIN;",
    ""
]

# 添加更新语句
updated_count = 0
missing_translation = []

for city in cities_to_update:
    if city['new_name']:
        # 转义单引号
        old_name = city['old_name'].replace("'", "''")
        new_name = city['new_name'].replace("'", "''")
        country = city['country'].replace("'", "''")
        
        sql_lines.append(f"UPDATE cities SET name = '{new_name}' WHERE name = '{old_name}' AND country = '{country}';")
        updated_count += 1
    else:
        missing_translation.append(f"-- TODO: {city['old_name']} ({city['country']})")

sql_lines.append("")
sql_lines.append("COMMIT;")
sql_lines.append("")
sql_lines.append(f"-- 成功生成 {updated_count} 条更新语句")

if missing_translation:
    sql_lines.append("")
    sql_lines.append("-- =====================================================")
    sql_lines.append("-- 以下城市缺少中文翻译,需要手动添加:")
    sql_lines.append("-- =====================================================")
    sql_lines.extend(missing_translation)

sql_lines.append("")
sql_lines.append("-- 查看更新结果")
sql_lines.append("SELECT name, country FROM cities WHERE name ~ '[a-zA-Z]' ORDER BY country, name;")

# 写入 SQL 文件
sql_content = '\n'.join(sql_lines)
with open(r'e:\Workspaces\WaldenProjects\go-nomads\database\migrations\update_cities_name_to_chinese.sql', 'w', encoding='utf-8') as f:
    f.write(sql_content)

print("✅ SQL 脚本已生成!")
print("📊 统计信息:")
print(f"   - 总城市数: {len(cities)}")
print(f"   - 包含英文的城市: {len(cities_to_update)}")
print(f"   - 可以更新的城市: {updated_count}")
print(f"   - 缺少翻译的城市: {len(missing_translation)}")
print("\n📄 文件位置: e:\\Workspaces\\WaldenProjects\\go-nomads\\database\\migrations\\update_cities_name_to_chinese.sql")

if missing_translation:
    print("\n⚠️  以下城市需要手动添加中文翻译:")
    for item in missing_translation[:10]:  # 只显示前10个
        print(f"   {item}")
    if len(missing_translation) > 10:
        print(f"   ... 还有 {len(missing_translation) - 10} 个")
